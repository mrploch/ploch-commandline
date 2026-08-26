using Moq;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for the executor that wraps <see cref="ICommandApp" />. The pause-before-exit prompt used to be applied
///     by <see cref="CommandAppExecutor.RunAsync" /> only, so the synchronous entry point silently ignored the setting.
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class CommandAppExecutorTests : IDisposable
{
    private readonly TextReader _originalInput = Console.In;
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;

    public void Dispose()
    {
        Console.SetIn(_originalInput);
        AnsiConsole.Console = _originalConsole;
        EnvironmentSettings.Reset();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Run_should_return_the_exit_code_produced_by_the_command_app()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(7);
        SetPauseBeforeExit(false);
        string[] expectedArguments = ["build", "--verbose"];

        new CommandAppExecutor(commandApp.Object, CancellationToken.None).Run("build", "--verbose").Should().Be(7);

        commandApp.Verify(app => app.Run(It.Is<IEnumerable<string>>(args => args.SequenceEqual(expectedArguments)), It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task RunAsync_should_return_the_exit_code_produced_by_the_command_app()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);
        SetPauseBeforeExit(false);

        var result = await new CommandAppExecutor(commandApp.Object, CancellationToken.None).RunAsync("build");

        result.Should().Be(3);
    }

    [Fact]
    public void Run_should_not_prompt_when_pausing_before_exit_is_disabled()
    {
        using var console = UseRecordingConsole();
        SetPauseBeforeExit(false);

        new CommandAppExecutor(Mock.Of<ICommandApp>(), CancellationToken.None).Run("build");

        console.Output.Should().BeEmpty("an ordinary invocation must not appear to hang waiting for Enter");
    }

    [Fact]
    public void Run_should_prompt_and_wait_for_input_when_pausing_before_exit_is_enabled()
    {
        using var console = UseRecordingConsole();
        SetPauseBeforeExit(true);
        var input = new TrackingReader();
        Console.SetIn(input);

        new CommandAppExecutor(Mock.Of<ICommandApp>(), CancellationToken.None).Run("build");

        console.Output.Should().Contain("Press Enter to exit...");
        input.ReadLineCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_should_prompt_and_wait_for_input_when_pausing_before_exit_is_enabled()
    {
        using var console = UseRecordingConsole();
        SetPauseBeforeExit(true);
        var input = new TrackingReader();
        Console.SetIn(input);

        await new CommandAppExecutor(Mock.Of<ICommandApp>(), CancellationToken.None).RunAsync("build");

        console.Output.Should().Contain("Press Enter to exit...");
        input.ReadLineCount.Should().Be(1, "Run and RunAsync must honour the setting identically");
    }

    [Fact]
    public void Run_should_hand_Spectre_the_token_it_was_built_with()
    {
        var commandApp = new Mock<ICommandApp>();
        CancellationToken received = default;
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<string>, CancellationToken>((_, token) => received = token)
                  .Returns(0);
        SetPauseBeforeExit(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        new CommandAppExecutor(commandApp.Object, cancellationTokenSource.Token).Run("build");

        received.CanBeCanceled.Should().BeTrue("a token that can never be cancelled makes the whole feature inert");
        received.IsCancellationRequested.Should().BeFalse();

        cancellationTokenSource.Cancel();

        received.IsCancellationRequested
                .Should()
                .BeTrue("cancelling the source the application was built with must reach the running command");
    }

    [Fact]
    public async Task RunAsync_should_hand_Spectre_the_token_it_was_built_with()
    {
        var commandApp = new Mock<ICommandApp>();
        CancellationToken received = default;
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<string>, CancellationToken>((_, token) => received = token)
                  .ReturnsAsync(0);
        SetPauseBeforeExit(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        await new CommandAppExecutor(commandApp.Object, cancellationTokenSource.Token).RunAsync("build");

        received.CanBeCanceled.Should().BeTrue();

        await cancellationTokenSource.CancelAsync();

        received.IsCancellationRequested.Should().BeTrue();
    }

    private static void SetPauseBeforeExit(bool pauseBeforeExit) =>
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit, new Dictionary<string, string?>());

    private static RecordingConsole UseRecordingConsole()
    {
        var console = new RecordingConsole();
        AnsiConsole.Console = console.Console;

        return console;
    }

    /// <summary>A stdin stand-in that returns end-of-input immediately and records that it was consulted.</summary>
    private sealed class TrackingReader : TextReader
    {
        public int ReadLineCount { get; private set; }

        public override string? ReadLine()
        {
            ReadLineCount++;

            return null;
        }
    }
}
