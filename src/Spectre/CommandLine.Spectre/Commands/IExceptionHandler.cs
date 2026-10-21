namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Defines a contract for handling exceptions that occur during command execution, and - through the handler
///     <see cref="AppBuilder.ConfigureCommandApp" /> installs - a failure before the command runs that is not a parse,
///     binding or validation failure, such as an exception from a command interceptor or from a command that does not
///     derive from <see cref="AppCommand{TSettings}" /> or <see cref="AsyncAppCommand{TSettings}" />.
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
