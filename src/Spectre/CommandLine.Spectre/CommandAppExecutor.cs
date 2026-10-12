using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Implements the <see cref="ICommandAppExecutor" /> interface to execute command-line applications.
/// </summary>
/// <param name="commandApp">The command application to execute.</param>
/// <param name="cancellationToken">
///     The token handed to Spectre, and through it to every command. <see cref="AppBuilder.Create" /> cancels the
///     source behind this token when the user interrupts the application, so a command that honours its
///     <see cref="CancellationToken" /> stops when they do.
/// </param>
/// <remarks>
///     The token is taken rather than the <see cref="CancellationTokenSource" /> behind it. This type only ever
///     observes cancellation, it never requests it, so it has no use for the source. Taking the token also decouples
///     execution from the source's lifetime: <see cref="CancellationTokenSource.Token" /> throws
///     <see cref="ObjectDisposedException" /> once the source is disposed, whereas a token captured beforehand stays
///     usable.
///     <para>
///         An executor permits one execution attempt. An attempt may build the host behind the type registrar, and
///         <see cref="IHostBuilder.Build" /> cannot be called again, so every attempt consumes the executor whatever its
///         outcome: a second <see cref="Run" /> or <see cref="RunAsync" /> is rejected up front with an
///         <see cref="InvalidOperationException" />. Running again takes a new executor over a freshly configured
///         command application; <see cref="AppBuilder.ConfigureCommandApp" /> does both when the application was built
///         through <see cref="AppBuilder" />. An executor constructed directly, or through
///         <see cref="CommandAppConfigurator" />, wraps an <see cref="ICommandApp" /> the caller owns, so the caller
///         configures a new one.
///     </para>
/// </remarks>
public class CommandAppExecutor(ICommandApp commandApp, CancellationToken cancellationToken) : ICommandAppExecutor
{
    private const string AlreadyRunMessage =
        "This ICommandAppExecutor has already been used. An executor permits one execution attempt, because an attempt may build "
        + "the host behind it and that host cannot be built again. To run again, build a new executor over a freshly configured "
        + "command application: call AppBuilder.ConfigureCommandApp again when the application was built through AppBuilder, "
        + "or configure a new ICommandApp when the executor was constructed directly.";

    /// <summary>1 once the execution attempt has been claimed. Shared by <see cref="Run" /> and <see cref="RunAsync" />.</summary>
    private int _runClaimed;

    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this executor has already been used; see the class remarks.</exception>
    public int Run(params IEnumerable<string> args)
    {
        ClaimRun();

        var result = commandApp.Run(args, cancellationToken);

        PauseBeforeExitIfRequested();

        return result;
    }

    /// <summary>
    ///     Executes the command-line application asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with an integer result representing the exit code of the application, where 0 typically indicates
    ///     success.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when this executor has already been used; see the class remarks.</exception>
    public Task<int> RunAsync(params IEnumerable<string> args)
    {
        // Claimed before the async state machine starts, so a rejected call throws synchronously, as a usage error
        // should, rather than handing back a faulted task.
        ClaimRun();

        return RunClaimedAsync(args);
    }

    /// <summary>The asynchronous part of <see cref="RunAsync" />, entered only once the attempt has been claimed.</summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>A task whose result is the exit code of the application.</returns>
    private async Task<int> RunClaimedAsync(IEnumerable<string> args)
    {
        var result = await commandApp.RunAsync(args, cancellationToken).ConfigureAwait(false);

        PauseBeforeExitIfRequested();

        return result;
    }

    /// <summary>
    ///     Claims the executor's one execution attempt, or throws when it has already been claimed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the attempt has already been claimed.</exception>
    private void ClaimRun()
    {
        // Claimed on entry rather than on success: an attempt that failed may already have built the host, so a retry
        // on the same executor could never be a fresh start. Interlocked so an overlapping call is rejected rather than
        // racing the one in flight (PLO-756).
        if (Interlocked.Exchange(ref _runClaimed, 1) == 1)
        {
            throw new InvalidOperationException(AlreadyRunMessage);
        }
    }

    /// <summary>
    ///     Waits for the user to press Enter when <see cref="EnvironmentSettings.PauseBeforeExit" /> is set and the
    ///     run was not cancelled. Applied identically by <see cref="Run" /> and <see cref="RunAsync" />.
    /// </summary>
    private void PauseBeforeExitIfRequested()
    {
        if (!EnvironmentSettings.Current.PauseBeforeExit)
        {
            return;
        }

        // Skipped after cancellation: the user has already asked the application to stop, so prompting them to press
        // Enter before it will exit turns a requested shutdown into a hang.
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        AnsiConsole.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
