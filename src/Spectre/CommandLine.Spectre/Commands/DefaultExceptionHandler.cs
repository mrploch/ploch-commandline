using System.ComponentModel;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Default implementation of the <see cref="IExceptionHandler" /> interface that writes exceptions to the console.
/// </summary>
/// <param name="console">The ANSI console used to display exception information.</param>
/// <param name="output">The output writer used to render exception details.</param>
public class DefaultExceptionHandler(IAnsiConsole console, IOutput output) : IExceptionHandler
{
    /// <summary>
    ///     Handles an exception by writing its details to the console.
    /// </summary>
    /// <param name="ex">The exception that was thrown during command execution.</param>
    /// <returns>
    ///     An error exit code indicating that the command execution failed.
    /// </returns>
    public int HandleException(Exception ex)
    {
        if (ex is Win32Exception || ex.InnerException is Win32Exception)
        {
            // Written through IAnsiConsole.WriteLine rather than the markup-aware output: a Win32 exception's
            // ToString() contains '[' sequences that Spectre would otherwise parse as markup and fail on,
            // inside the exception handler itself.
            console.WriteLine(ex.ToString());
        }
        else
        {
            output.WriteException(ex);
        }

        return (int)ExitCode.Error;
    }
}
