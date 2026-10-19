namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Defines an interface for executing command-line applications.
/// </summary>
/// <remarks>
///     <para>
///         An executor permits one execution attempt: an attempt may build the host it runs over, and that host cannot be
///         built again, so every attempt consumes the executor whatever its outcome. A second <see cref="Run" /> or
///         <see cref="RunAsync" /> on the same instance throws <see cref="InvalidOperationException" />. To run again,
///         build a new executor over a freshly configured command application — <see cref="AppBuilder.ConfigureCommandApp" />
///         does both when the application was built through <see cref="AppBuilder" />.
///     </para>
///     <para>
///         For an executor returned by <see cref="AppBuilder.ConfigureCommandApp" />, a run that reaches command
///         resolution builds the <see cref="Microsoft.Extensions.Hosting.IHost" /> behind it, for dependency injection,
///         configuration and logging only; a parse failure or <c>--version</c> returns before any host exists. The library
///         never starts that host: a CLI command is the unit of work, so it invokes neither <c>StartAsync</c> nor
///         <c>StopAsync</c> on a registered <see cref="Microsoft.Extensions.Hosting.IHostedService" />, and it disposes a
///         host it built when the run ends. Starting the host around the command is a possible 1.x feature, tracked as
///         <see href="https://linear.app/ploch/issue/PLO-785">PLO-785</see>.
///     </para>
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
    /// <exception cref="InvalidOperationException">
    ///     Thrown synchronously, before any task is returned, when this executor has already been used.
    /// </exception>
    Task<int> RunAsync(params IEnumerable<string> args);
}
