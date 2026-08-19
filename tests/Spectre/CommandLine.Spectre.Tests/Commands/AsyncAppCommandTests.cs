using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the asynchronous command base: the settings processor runs before the implementation, the
///     cancellation token is forwarded, cancellation is distinguished from failure, and exceptions reach the
///     configured handler.
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
    public async Task ExecuteAsync_should_route_an_exception_to_the_exception_handler(CommandContext context)
    {
        var handler = new RecordingExceptionHandler();
        var command = CreateCommand((_, _) => throw new InvalidOperationException("boom"), handler);

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        handler.Handled.Should().ContainSingle().Which.Message.Should().Be("boom");
        result.Should().Be((int)ExitCode.Error);
    }

    private static StubAsyncCommand CreateCommand(Func<StubSettings, CancellationToken, Task<ExitCode>> body,
                                                  IExceptionHandler? exceptionHandler = null,
                                                  CommandArgumentsRootProcessor? processor = null) =>
        new(processor ?? new([]), new PassThroughValidator(), exceptionHandler ?? new RecordingExceptionHandler(), new NullOutput(), body);

    private sealed class StubSettings : CommandSettings
    {
    }

    private sealed class PassThroughValidator : ICommandSettingsValidator<StubSettings>
    {
        public ValidationResult Validate(CommandContext context, StubSettings settings) => ValidationResult.Success();
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
