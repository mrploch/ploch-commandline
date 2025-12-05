using Microsoft.Extensions.Logging;
using Ploch.Common.ArgumentChecking;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Handles exceptions that occur during command execution for a specific command type.
/// </summary>
/// <typeparam name="TCommand">The type of command for which exceptions are being handled.</typeparam>
/// <param name="console">The ANSI console used to display exception information.</param>
/// <param name="logger">The logger used to log exception details.</param>
public class ExceptionHandler<TCommand>(IAnsiConsole console, ILogger<TCommand> logger) : IExceptionHandler
{
    /// <summary>
    ///     Handles an exception by writing it to the console and logging it.
    /// </summary>
    /// <param name="ex">The exception to handle. Cannot be null.</param>
    /// <returns>An integer representing the exit code to return to the operating system.</returns>
    public virtual int HandleException(Exception ex)
    {
        ex.NotNull();

        console.WriteException(ex);
        logger.LogError(ex, "An error occurred while executing the {CommandType} command", typeof(TCommand).Name);

        return GetExitCode(ex);
    }

    /// <summary>
    ///     Determines the appropriate exit code based on the exception.
    /// </summary>
    /// <param name="ex">The exception for which to determine the exit code.</param>
    /// <returns>An integer representing the exit code, defaulting to the Error exit code.</returns>
    protected virtual int GetExitCode(Exception ex) => (int)ExitCode.Error;
}
