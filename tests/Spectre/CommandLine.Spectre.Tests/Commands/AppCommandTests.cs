using System.Globalization;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the synchronous command base. It performs the same framework work as
///     <see cref="AsyncAppCommand{TSettings}" />: the execution banner is printed, the settings processor runs before the
///     implementation, exceptions are routed to the configured handler rather than propagating, cancellation requested
///     through the supplied token is reported as <see cref="ExitCode.Cancelled" /> instead of being treated as a generic
///     failure while an <see cref="OperationCanceledException" /> nobody asked for is a fault like any other, and the
///     cancellation token reaches the implementation.
/// </summary>
public class AppCommandTests
{
    [Theory]
    [AutoMockData]
    public void Execute_should_return_the_exit_code_produced_by_the_implementation(CommandContext context)
    {
        var command = new StubCommand(_ => ExitCode.Success);

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_run_the_settings_processor_before_the_implementation(CommandContext context)
    {
        var order = new List<string>();
        var command = new StubCommand(_ =>
                                      {
                                          order.Add("execute");

                                          return ExitCode.Success;
                                      },
                                      processor: new([new RecordingProcessor(order)]));

        command.Execute(context, new StubSettings(), CancellationToken.None);

        order.Should().Equal("process", "execute");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_pass_the_settings_instance_to_the_settings_processor(CommandContext context)
    {
        var processor = new RecordingProcessor([]);
        var settings = new StubSettings();
        var command = new StubCommand(_ => ExitCode.Success, processor: new([processor]));

        command.Execute(context, settings, CancellationToken.None);

        processor.Received.Should().ContainSingle().Which.Should().BeSameAs(settings);
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_print_the_execution_banner_before_processing_the_arguments(CommandContext context)
    {
        var lines = new List<string>();
        var command = new StubCommand(_ =>
                                      {
                                          lines.Add("execute");

                                          return ExitCode.Success;
                                      },
                                      processor: new([new RecordingProcessor(lines)]),
                                      output: new RecordingOutput(lines));

        command.Execute(context, new StubSettings(), CancellationToken.None);

        lines.Should()
             .Equal($"Executing command [bold underline]{nameof(StubSettings)}[/]", string.Empty, "Processing arguments...", "process", "execute");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_an_exception_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(_ => throw new InvalidOperationException("boom"), handler);

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        handler.Handled.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
        result.Should().Be((int)ExitCode.Error);
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_a_settings_processor_exception_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var executed = false;
        var command = new StubCommand(_ =>
                                      {
                                          executed = true;

                                          return ExitCode.Success;
                                      },
                                      handler,
                                      new([new ThrowingProcessor()]));

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Message.Should().Be("processing failed");
        executed.Should().BeFalse("the implementation must not run when argument processing fails");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_report_cancellation_without_involving_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(token => throw new OperationCanceledException(token), handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = command.Execute(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled);
        handler.Handled.Should().BeEmpty("cancellation is a requested outcome, not a fault");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_a_banner_output_failure_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var output = new Mock<IOutput>();
        output.Setup(o => o.MarkupLineInterpolated(It.IsAny<FormattableString>())).Throws(new IOException("console gone"));
        var executed = false;
        var command = new StubCommand(_ =>
                                      {
                                          executed = true;

                                          return ExitCode.Success;
                                      },
                                      handler,
                                      output: output.Object);

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeOfType<IOException>();
        executed.Should().BeFalse();
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_report_cancellation_from_the_settings_processor_without_involving_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(_ => ExitCode.Success, handler, new([new CancellingProcessor()]));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = command.Execute(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled);
        handler.Handled.Should().BeEmpty("cancellation during argument processing is a requested outcome, not a fault");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_cancellation_from_the_settings_processor_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(_ => ExitCode.Success, handler, new([new CancellingProcessor()]));

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeOfType<OperationCanceledException>();
    }

    // Pins the fix for PLO-756: an OperationCanceledException is only "the user cancelled" when the token the command
    // received was cancelled. One raised for any other reason - here an unrelated token the command itself observed -
    // is a fault and must reach the handler like every other exception. (A plain comment rather than XML docs: the
    // documentation analysers would demand <param> and <returns> on a test method.)
    [Theory]
    [AutoMockData]
    public void Execute_should_route_an_OperationCanceledException_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var foreign = new CancellationTokenSource();
        foreign.Cancel();
        var exception = new OperationCanceledException(foreign.Token);
        var command = new StubCommand(_ => throw exception, handler);

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error, "a cancellation nobody asked for is a fault, not a requested outcome");
        handler.Handled.Should().ContainSingle().Which.Should().BeSameAs(exception, "the handler must see the original exception");
    }

    // Cancellation requested while the command is running, not before: the filter reads the token's state at catch time.
    [Theory]
    [AutoMockData]
    public void Execute_should_report_cancellation_requested_while_the_command_is_running(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var cts = new CancellationTokenSource();

        // The delegate is captured rather than the source, so the lambda never touches a disposed object.
        Action requestCancellation = cts.Cancel;
        var command = new StubCommand(token =>
                                      {
                                          requestCancellation();
                                          token.ThrowIfCancellationRequested();

                                          return ExitCode.Success;
                                      },
                                      handler);

        var result = command.Execute(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled, "the token's state is read when the exception is caught, not when the command started");
        handler.Handled.Should().BeEmpty();
    }

    // The token shape AppBuilder actually supplies: cancellable, but not cancelled. Pins that the filter reads
    // IsCancellationRequested and not CanBeCanceled, which CancellationToken.None alone could not tell apart.
    [Theory]
    [AutoMockData]
    public void Execute_should_route_an_OperationCanceledException_to_the_exception_handler_when_a_cancellable_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        using var foreign = new CancellationTokenSource();
        foreign.Cancel();
        var exception = new OperationCanceledException(foreign.Token);
        var command = new StubCommand(_ => throw exception, handler);
        using var live = new CancellationTokenSource();

        var result = command.Execute(context, new StubSettings(), live.Token);

        result.Should().Be((int)ExitCode.Error, "a token that merely can be cancelled has not been");
        handler.Handled.Should().ContainSingle().Which.Should().BeSameAs(exception);
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_a_TaskCanceledException_to_the_exception_handler_when_the_token_was_not_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var exception = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout elapsing.");
        var command = new StubCommand(_ => throw exception, handler);

        var result = command.Execute(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        handler.Handled.Should().ContainSingle().Which.Should().BeSameAs(exception);
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_report_cancellation_propagated_through_a_linked_token(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(token =>
                                      {
                                          using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
                                          linked.Token.ThrowIfCancellationRequested();

                                          return ExitCode.Success;
                                      },
                                      handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = command.Execute(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Cancelled, "a linked token carries a different token value, but the cancellation was still requested");
        handler.Handled.Should().BeEmpty();
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_route_an_exception_other_than_cancellation_to_the_exception_handler_even_when_the_token_was_cancelled(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = new StubCommand(_ => throw new InvalidOperationException("boom"), handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = command.Execute(context, new StubSettings(), cts.Token);

        result.Should().Be((int)ExitCode.Error, "only an OperationCanceledException is a requested outcome; the filter must not widen");
        handler.Handled.Should().ContainSingle().Which.Message.Should().Be("boom");
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_forward_the_cancellation_token_to_the_implementation(CommandContext context)
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;
        var command = new StubCommand(token =>
                                      {
                                          received = token;

                                          return ExitCode.Success;
                                      });

        command.Execute(context, new StubSettings(), cts.Token);

        received.Should().Be(cts.Token);
    }

    [Fact]
    public void Execute_should_throw_when_context_is_null()
    {
        var command = new StubCommand(_ => ExitCode.Success);

        var act = () => command.Execute(null!, new StubSettings(), CancellationToken.None);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [AutoMockData]
    public void Execute_should_throw_when_settings_are_null(CommandContext context)
    {
        var command = new StubCommand(_ => ExitCode.Success);

        var act = () => command.Execute(context, null!, CancellationToken.None);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_return_the_result_produced_by_the_configured_validator(CommandContext context)
    {
        var command = new StubCommand(_ => ExitCode.Success, validator: new RejectingValidator());

        var result = command.Validate(context, new StubSettings());

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("settings rejected");
    }

    [Fact]
    public void Output_should_expose_the_output_passed_to_the_constructor()
    {
        var output = new RecordingOutput([]);
        var command = new StubCommand(_ => ExitCode.Success, output: output);

        command.ExposedOutput.Should().BeSameAs(output);
    }

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

    private sealed class RecordingProcessor(List<string> order) : ICommandSettingsProcessor
    {
        public List<CommandSettings> Received { get; } = [];

        public void ProcessArguments(CommandSettings arguments)
        {
            Received.Add(arguments);
            order.Add("process");
        }
    }

    private sealed class ThrowingProcessor : ICommandSettingsProcessor
    {
        public void ProcessArguments(CommandSettings arguments) => throw new InvalidOperationException("processing failed");
    }

    private sealed class CancellingProcessor : ICommandSettingsProcessor
    {
        public void ProcessArguments(CommandSettings arguments) => throw new OperationCanceledException();
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

    private sealed class StubCommand(Func<CancellationToken, ExitCode> body,
                                     IExceptionHandler? exceptionHandler = null,
                                     CommandArgumentsRootProcessor? processor = null,
                                     IOutput? output = null,
                                     ICommandSettingsValidator<StubSettings>? validator = null)
        : AppCommand<StubSettings>(processor ?? new([]),
                                   validator ?? new PassThroughValidator(),
                                   exceptionHandler ?? new RecordingExceptionHandler(),
                                   output ?? new RecordingOutput([]))
    {
        public IOutput ExposedOutput => Output;

        protected override ExitCode DoExecute(CommandContext context, StubSettings settings, CancellationToken cancellationToken) => body(cancellationToken);
    }

    /// <summary>
    ///     An <see cref="IOutput" /> that records the text of each line-level call, so tests can assert on the banner
    ///     without writing to the console.
    /// </summary>
    private sealed class RecordingOutput(List<string> lines) : IOutput
    {
        public IOutput EndLine() => this;

        public IOutput Markup(string value) => this;

        public IOutput MarkupLine(string value)
        {
            lines.Add(value);

            return this;
        }

        public IOutput MarkupInterpolated(FormattableString value) => this;

        public IOutput MarkupLineInterpolated(FormattableString value)
        {
            lines.Add(value.ToString(CultureInfo.InvariantCulture));

            return this;
        }

        public IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null) => this;

        public IOutput Write(IRenderable renderable) => this;

        public IOutput WriteBold<TMessage>(TMessage? message) => this;

        public IOutput WriteBoldLine<TMessage>(TMessage? message) => this;

        public IOutput WriteError<TMessage>(TMessage? message) => this;

        public IOutput WriteErrorLine<TMessage>(TMessage? message) => this;

        public IOutput WriteException<TException>(TException? exception) where TException : Exception => this;

        public IOutput WriteLine()
        {
            lines.Add(string.Empty);

            return this;
        }

        public IOutput WriteLine<TMessage>(TMessage message)
        {
            lines.Add(message?.ToString() ?? string.Empty);

            return this;
        }
    }
}
