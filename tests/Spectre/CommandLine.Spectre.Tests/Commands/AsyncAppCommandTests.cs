using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the asynchronous command base: the settings processor runs before the implementation, the
///     cancellation token is forwarded, cancellation requested through the supplied token is distinguished from
///     failure while an <see cref="OperationCanceledException" /> nobody asked for is a fault like any other, and
///     exceptions reach the configured handler.
/// </summary>
public class AsyncAppCommandTests
{
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_return_the_exit_code_produced_by_the_implementation(CommandContext context)
    {
        var command = CreateCommand((_, _) => Task.FromResult(ExitCode.Success));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_run_the_settings_processor_before_the_implementation(CommandContext context)
    {
        var order = new List<string>();
        var processor = new RecordingProcessor(order);
        var command = CreateCommand((_, _) =>
                                    {
                                        order.Add("execute");

                                        return Task.FromResult(ExitCode.Success);
                                    },
                                    processor: new([processor]));

        await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        order.Should().Equal("process", "execute");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_forward_the_cancellation_token_to_the_implementation(CommandContext context)
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;
        var command = CreateCommand((_, token) =>
                                    {
                                        received = token;

                                        return Task.FromResult(ExitCode.Success);
                                    });

        await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        received.Should().Be(cts.Token);
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_cancellation_without_involving_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, token) => throw new OperationCanceledException(token), handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled);
        handler.Handled.Should().BeEmpty();
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_cancellation_from_the_settings_processor_without_involving_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, _) => Task.FromResult(ExitCode.Success), handler, new([new CancellingProcessor()]));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled);
        handler.Handled.Should().BeEmpty("cancellation during argument processing is a requested outcome, not a fault");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_cancellation_from_the_settings_processor_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, _) => Task.FromResult(ExitCode.Success), handler, new([new CancellingProcessor()]));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeOfType<OperationCanceledException>();
    }

    /// <summary>
    ///     Pins the fix for PLO-756: an <see cref="OperationCanceledException" /> is only "the user cancelled" when the
    ///     token the command received was cancelled. One raised for any other reason - here an unrelated token the
    ///     command itself observed - is a fault and must reach the handler like every other exception.
    /// </summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_an_OperationCanceledException_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var foreign = new CancellationTokenSource();
        await foreign.CancelAsync();
        var exception = new OperationCanceledException(foreign.Token);
        var command = CreateCommand((_, _) => throw exception, handler);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error, "a cancellation nobody asked for is a fault, not a requested outcome");
        handler.Handled.Should().ContainSingle().Which.Should().BeSameAs(exception, "the handler must see the original exception");
    }

    /// <summary>
    ///     The shape an <c>HttpClient</c> timeout takes: a <see cref="TaskCanceledException" /> while the command's own
    ///     token is live. Reporting it as exit code 130 would tell the user they cancelled something they did not.
    /// </summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_a_TaskCanceledException_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var exception = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout elapsing.");
        var command = CreateCommand((_, _) => throw exception, handler);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeSameAs(exception);
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_a_cancelled_task_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var foreign = new CancellationTokenSource();
        await foreign.CancelAsync();
        var command = CreateCommand((_, _) => Task.FromCanceled<ExitCode>(foreign.Token), handler);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error, "cancellation observed through await is still a fault when nobody cancelled the command");
        handler.Handled.Should().ContainSingle().Which.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_cancellation_observed_through_a_cancelled_task(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, token) => Task.FromCanceled<ExitCode>(token), handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled, "a task cancelled with the supplied token is the ordinary async shape of a requested cancellation");
        handler.Handled.Should().BeEmpty();
    }

    /// <summary>Cancellation requested while the command is suspended, not before: the filter reads the token's state at catch time.</summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_cancellation_requested_while_the_command_is_running(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource();
        var command = CreateCommand(async (_, token) =>
                                    {
                                        started.SetResult();
                                        await Task.Delay(Timeout.InfiniteTimeSpan, token);

                                        return ExitCode.Success;
                                    },
                                    handler);

        var execution = command.ExecuteAsync(context, new StubSettings(), cts.Token);
        await started.Task;
        await cts.CancelAsync();

        (await execution).Should().Be((int)ExitCode.Cancelled, "the token's state is read when the exception is caught, not when the command started");
        handler.Handled.Should().BeEmpty();
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_cancellation_propagated_through_a_linked_token(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, token) =>
                                    {
                                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
                                        linked.Token.ThrowIfCancellationRequested();

                                        return Task.FromResult(ExitCode.Success);
                                    },
                                    handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled, "a linked token carries a different token value, but the cancellation was still requested");
        handler.Handled.Should().BeEmpty();
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_an_exception_other_than_cancellation_to_the_exception_handler_even_when_the_token_was_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, _) => throw new InvalidOperationException("boom"), handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await command.ExecuteAsync(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Error, "only an OperationCanceledException is a requested outcome; the filter must not widen");
        handler.Handled.Should().ContainSingle().Which.Message.Should().Be("boom");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_an_exception_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, _) => throw new InvalidOperationException("boom"), handler);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        handler.Handled.Should().ContainSingle().Which.Message.Should().Be("boom");
        result.Should().Be((int)ExitCode.Error);
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_route_a_banner_output_failure_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var output = new Mock<IOutput>();
        output.Setup(o => o.MarkupLineInterpolated(It.IsAny<FormattableString>())).Throws(new IOException("console gone"));
        var executed = false;
        var command = CreateCommand((_, _) =>
                                    {
                                        executed = true;

                                        return Task.FromResult(ExitCode.Success);
                                    },
                                    handler,
                                    output: output.Object);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeOfType<IOException>();
        executed.Should().BeFalse();
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_return_the_result_produced_by_the_configured_validator(CommandContext context)
    {
        var command = CreateCommand((_, _) => Task.FromResult(ExitCode.Success), validator: new RejectingValidator());

        var result = command.Validate(context, new StubSettings());

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("settings rejected");
    }

    private static StubAsyncCommand CreateCommand(Func<StubSettings, CancellationToken, Task<ExitCode>> body,
                                                  IExceptionHandler? exceptionHandler = null,
                                                  CommandArgumentsRootProcessor? processor = null,
                                                  ICommandSettingsValidator<StubSettings>? validator = null,
                                                  IOutput? output = null) =>
        new(processor ?? new([]), validator ?? new PassThroughValidator(), exceptionHandler ?? new RecordingExceptionHandler(), output ?? new NullOutput(), body);

    private sealed class StubSettings : CommandSettings
    {
    }

    private sealed class PassThroughValidator : ICommandSettingsValidator<StubSettings>
    {
        public ValidationResult Validate(CommandContext context, StubSettings settings) => ValidationResult.Success();
    }

    private sealed class RejectingValidator : ICommandSettingsValidator<StubSettings>
    {
        public ValidationResult Validate(CommandContext context, StubSettings settings) => ValidationResult.Error("settings rejected");
    }

    private sealed class RecordingExceptionHandler : IExceptionHandler
    {
        public List<Exception> Handled { get; } = [];

        public int HandleException(Exception ex)
        {
            Handled.Add(ex);

            return (int)ExitCode.Error;
        }
    }

    private sealed class RecordingProcessor(List<string> order) : ICommandSettingsProcessor
    {
        public void ProcessArguments(CommandSettings arguments) => order.Add("process");
    }

    private sealed class CancellingProcessor : ICommandSettingsProcessor
    {
        public void ProcessArguments(CommandSettings arguments) => throw new OperationCanceledException();
    }

    private sealed class StubAsyncCommand(CommandArgumentsRootProcessor settingsProcessor,
                                          ICommandSettingsValidator<StubSettings> validator,
                                          IExceptionHandler exceptionHandler,
                                          IOutput output,
                                          Func<StubSettings, CancellationToken, Task<ExitCode>> body)
        : AsyncAppCommand<StubSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        protected override Task<ExitCode> DoExecuteAsync(CommandContext context, StubSettings settings, CancellationToken cancellationToken) =>
            body(settings, cancellationToken);
    }

    /// <summary>An <see cref="IOutput" /> that discards everything, so tests do not write to the console.</summary>
    private sealed class NullOutput : IOutput
    {
        public IOutput EndLine() => this;

        public IOutput MarkupInterpolated(FormattableString value) => this;

        public IOutput MarkupLineInterpolated(FormattableString value) => this;

        public IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null) => this;

        public IOutput Write(IRenderable renderable) => this;

        public IOutput WriteBold<TMessage>(TMessage? message) => this;

        public IOutput WriteBoldLine<TMessage>(TMessage? message) => this;

        public IOutput WriteError<TMessage>(TMessage? message) => this;

        public IOutput WriteErrorLine<TMessage>(TMessage? message) => this;

        public IOutput WriteException<TException>(TException? exception) where TException : Exception => this;

        public IOutput WriteLine() => this;

        public IOutput WriteLine<TMessage>(TMessage message) => this;
    }
}
