using Microsoft.Extensions.Hosting;
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

    /// <summary>
    ///     Wiring the token up made this reachable: a cancelled run that also had PauseBeforeExit set would print
    ///     "Press Enter to exit..." and block on stdin, turning the shutdown the user just asked for into a hang.
    /// </summary>
    [Fact]
    public void Run_should_not_pause_for_input_when_the_run_was_cancelled()
    {
        using var console = UseRecordingConsole();
        SetPauseBeforeExit(true);
        var input = new TrackingReader();
        Console.SetIn(input);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        new CommandAppExecutor(Mock.Of<ICommandApp>(), cancellationTokenSource.Token).Run("build");

        input.ReadLineCount.Should().Be(0, "the user has already asked the application to stop");
        console.Output.Should().NotContain("Press Enter to exit...");
    }

    [Fact]
    public async Task RunAsync_should_not_pause_for_input_when_the_run_was_cancelled()
    {
        using var console = UseRecordingConsole();
        SetPauseBeforeExit(true);
        var input = new TrackingReader();
        Console.SetIn(input);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await new CommandAppExecutor(Mock.Of<ICommandApp>(), cancellationTokenSource.Token).RunAsync("build");

        input.ReadLineCount.Should().Be(0, "Run and RunAsync must honour cancellation identically");
        console.Output.Should().NotContain("Press Enter to exit...");
    }

    /// <summary>
    ///     PLO-756: an executor runs once. The host behind Spectre's type registrar is built on the first run and
    ///     <see cref="IHostBuilder.Build" /> is single-shot, so a second run could only ever fail; it now fails
    ///     up front with a message that says what to do instead of Spectre rendering "Build can only be called once".
    /// </summary>
    [Fact]
    public void Run_should_throw_when_called_a_second_time()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(0);
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        executor.Run("build");

        var secondRun = () => executor.Run("build");

        secondRun.Should().Throw<InvalidOperationException>().WithMessage("*ConfigureCommandApp*", "the message has to say how to run again");
        commandApp.Verify(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once, "a rejected run must never reach Spectre");
    }

    // Invoked through an Action and asserted with Throw, not ThrowAsync: the rejection is a usage error and is meant to
    // throw synchronously, before any task is handed back. ThrowAsync would also accept a faulted task, so it could not
    // tell the two apart. (A plain comment: Qodana's documentation rules would demand <returns> on an async test.)
    [Fact]
    public async Task RunAsync_should_throw_synchronously_when_called_a_second_time()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        await executor.RunAsync("build");

        Action secondRun = () => _ = executor.RunAsync("build");

        secondRun.Should()
                 .Throw<InvalidOperationException>("a usage error throws before the task is created rather than faulting the task")
                 .WithMessage("*ConfigureCommandApp*");
        commandApp.Verify(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RunAsync_should_throw_synchronously_when_Run_has_already_been_called()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(0);
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        executor.Run("build");

        Action secondRun = () => _ = executor.RunAsync("build");

        secondRun.Should().Throw<InvalidOperationException>("Run and RunAsync share the one attempt the executor has");
        commandApp.Verify(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Run_should_throw_when_RunAsync_has_already_been_called()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        await executor.RunAsync("build");

        var secondRun = () => executor.Run("build");

        secondRun.Should().Throw<InvalidOperationException>();
        commandApp.Verify(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     The run is consumed on entry, not on success: a run that threw may already have built the host, so a retry
    ///     on the same executor cannot be allowed to look like a fresh start.
    /// </summary>
    [Fact]
    public void Run_should_not_be_retryable_after_the_command_app_threw()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Throws(new IOException("console gone"));
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        var firstRun = () => executor.Run("build");
        firstRun.Should().Throw<IOException>();

        var secondRun = () => executor.Run("build");

        secondRun.Should().Throw<InvalidOperationException>("the executor is used up by the attempt, whatever its outcome");
        commandApp.Verify(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_should_not_be_retryable_after_the_command_app_faulted()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new IOException("console gone"));
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        var firstRun = () => executor.RunAsync("build");
        await firstRun.Should().ThrowAsync<IOException>();

        var secondRun = () => executor.RunAsync("build");

        await secondRun.Should().ThrowAsync<InvalidOperationException>();
        commandApp.Verify(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_should_throw_while_a_RunAsync_is_still_in_progress()
    {
        var commandApp = new Mock<ICommandApp>();
        var running = new TaskCompletionSource<int>();
        commandApp.Setup(app => app.RunAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(running.Task);
        SetPauseBeforeExit(false);
        var executor = new CommandAppExecutor(commandApp.Object, CancellationToken.None);
        var pending = executor.RunAsync("build");

        var overlappingRun = () => executor.Run("build");

        overlappingRun.Should().Throw<InvalidOperationException>("the run is claimed on entry, so an overlapping call is rejected rather than racing the first");
        running.SetResult(0);
        (await pending).Should().Be(0);
        commandApp.Verify(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
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
