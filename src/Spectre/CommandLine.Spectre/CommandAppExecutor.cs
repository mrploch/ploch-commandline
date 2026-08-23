using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Implements the <see cref="ICommandAppExecutor" /> interface to execute command-line applications.
/// </summary>
/// <param name="commandApp">The command application to execute.</param>
/// <param name="cancellationTokenSource">
///     The source whose token is handed to Spectre, and through it to every command. This is the source
///     <see cref="AppBuilder.Create" /> cancels when the user interrupts the application, so a command that honours its
///     <see cref="CancellationToken" /> stops when they do.
/// </param>
public class CommandAppExecutor(ICommandApp commandApp, CancellationTokenSource cancellationTokenSource) : ICommandAppExecutor
{
    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    public int Run(params IEnumerable<string> args)
    {
        var result = commandApp.Run(args, cancellationTokenSource.Token);

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
        var result = await commandApp.RunAsync(args, cancellationTokenSource.Token).ConfigureAwait(false);

        PauseBeforeExitIfRequested();

        return result;
    }

    /// <summary>
    ///     Waits for the user to press Enter when <see cref="EnvironmentSettings.PauseBeforeExit" /> is set.
    ///     Applied identically by <see cref="Run" /> and <see cref="RunAsync" />.
    /// </summary>
    private static void PauseBeforeExitIfRequested()
    {
        if (!EnvironmentSettings.Current.PauseBeforeExit)
        {
            return;
        }

        AnsiConsole.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
