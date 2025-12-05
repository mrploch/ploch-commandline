namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Defines a contract for handling exceptions that occur during command execution.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    ///     Handles an exception that occurred during command execution.
    /// </summary>
    /// <param name="ex">The exception that was thrown during command execution.</param>
    /// <returns>
    ///     An integer representing the exit code to return to the operating system.
    ///     Typically, a non-zero value indicates an error condition.
    /// </returns>
    int HandleException(Exception ex);
}
