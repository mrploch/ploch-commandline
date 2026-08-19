using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Implements the <see cref="ICommandAppExecutor" /> interface to execute command-line applications.
/// </summary>
/// <param name="commandApp">The command application to execute.</param>
public class CommandAppExecutor(ICommandApp commandApp) : ICommandAppExecutor
{
    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    public int Run(params IEnumerable<string> args) => commandApp.Run(args);

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
        var result = await commandApp.RunAsync(args);

        if (EnvironmentSettings.Current.PauseBeforeExit)
        {
            AnsiConsole.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        return result;
    }
}
