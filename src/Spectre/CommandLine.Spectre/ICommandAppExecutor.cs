namespace Ploch.CommandLine.Spectre;

/// <summary>
/// Defines an interface for executing command-line applications.
/// </summary>
public interface ICommandAppExecutor
{
    /// <summary>
    /// Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    int Run(params IEnumerable<string> args);

    /// <summary>
    /// Executes the command-line application asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>A task representing the asynchronous operation, with an integer result representing the exit code of the application, where 0 typically indicates success.</returns>
    Task<int> RunAsync(params IEnumerable<string> args);
}
