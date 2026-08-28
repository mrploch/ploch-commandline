using Ploch.CommandLine.Spectre.Commands;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the synchronous command base: exceptions are routed to the configured handler rather than
///     propagating, cancellation is reported as <see cref="ExitCode.Cancelled" /> instead of being treated as a
///     generic failure, and the cancellation token reaches the implementation.
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

    [Theory]
    [AutoMockData]
    public void Execute_should_throw_when_settings_are_null(CommandContext context)
    {
        var command = new StubCommand(_ => ExitCode.Success);

        var act = () => command.Execute(context, null!, CancellationToken.None);

        act.Should().Throw<ArgumentNullException>();
    }

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

    private sealed class StubCommand(Func<CancellationToken, ExitCode> body, IExceptionHandler? exceptionHandler = null)
        : AppCommand<StubSettings>(new PassThroughValidator(), exceptionHandler ?? new RecordingExceptionHandler())
    {
        protected override ExitCode DoExecute(CommandContext context, StubSettings settings, CancellationToken cancellationToken) => body(cancellationToken);
    }
}
