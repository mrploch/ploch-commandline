namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Defines an interface for executing command-line applications.
/// </summary>
/// <remarks>
///     For an executor returned by <see cref="AppBuilder.ConfigureCommandApp" />, a run that reaches command
///     resolution builds the <see cref="Microsoft.Extensions.Hosting.IHost" /> behind it, for dependency injection,
///     configuration and logging only; a parse failure or <c>--version</c> returns before any host exists. The library
///     never starts that host: a CLI command is the unit of work, so it invokes neither <c>StartAsync</c> nor
///     <c>StopAsync</c> on a registered <see cref="Microsoft.Extensions.Hosting.IHostedService" />, and it disposes a
///     host it built when the run ends. Starting the host around the command is a possible 1.x feature, tracked as
///     <see href="https://linear.app/ploch/issue/PLO-785">PLO-785</see>.
/// </remarks>
public interface ICommandAppExecutor
{
    /// <summary>
    ///     Executes the command-line application synchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>An integer representing the exit code of the application, where 0 typically indicates success.</returns>
    int Run(params IEnumerable<string> args);

    /// <summary>
    ///     Executes the command-line application asynchronously with the specified arguments.
    /// </summary>
    /// <param name="args">Collections of command-line arguments to pass to the application.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with an integer result representing the exit code of the application, where 0 typically indicates
    ///     success.
    /// </returns>
    Task<int> RunAsync(params IEnumerable<string> args);
}
