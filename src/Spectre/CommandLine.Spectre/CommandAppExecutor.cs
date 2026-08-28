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
/// </remarks>
public class CommandAppExecutor(ICommandApp commandApp, CancellationToken cancellationToken) : ICommandAppExecutor
{
    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    public int Run(params IEnumerable<string> args)
    {
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
    public async Task<int> RunAsync(params IEnumerable<string> args)
    {
        var result = await commandApp.RunAsync(args, cancellationToken).ConfigureAwait(false);

        PauseBeforeExitIfRequested();

        return result;
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
