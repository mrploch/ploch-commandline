using Microsoft.Extensions.Logging;
using Ploch.Common;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Commands;

public class ExceptionHandler<TCommand>(IAnsiConsole console, ILogger<TCommand> logger) : IExceptionHandler<TCommand>
{
    public virtual int HandleException(Exception ex)
    {
        ex.NotNull();

        console.WriteException(ex);
        logger.LogError(ex, "An error occurred while executing the {CommandType} command", typeof(TCommand).Name);

        return GetExitCode(ex);
    }

    protected virtual int GetExitCode(Exception ex) => (int)ExitCode.Error;
}
