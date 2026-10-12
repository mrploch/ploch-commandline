namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Defines an interface for executing command-line applications.
/// </summary>
/// <remarks>
///     An executor permits one execution attempt: an attempt may build the host it runs over, and that host cannot be
///     built again, so every attempt consumes the executor whatever its outcome. A second <see cref="Run" /> or
///     <see cref="RunAsync" /> on the same instance throws <see cref="InvalidOperationException" />. To run again,
///     build a new executor over a freshly configured command application — <see cref="AppBuilder.ConfigureCommandApp" />
///     does both when the application was built through <see cref="AppBuilder" />.
/// </remarks>
public interface ICommandAppExecutor
{
    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this executor has already been used.</exception>
    int Run(params IEnumerable<string> args);

    /// <summary>
    ///     Executes the command-line application asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with an integer result representing the exit code of the application, where 0 typically indicates
    ///     success.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when this executor has already been used.</exception>
    Task<int> RunAsync(params IEnumerable<string> args);
}
