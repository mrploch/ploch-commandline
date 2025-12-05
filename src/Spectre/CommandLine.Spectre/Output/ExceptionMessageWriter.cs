using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Specialized message writer for handling and displaying exceptions.
/// </summary>
/// <param name="output">The output interface used to write exception messages.</param>
public class ExceptionMessageWriter(IAnsiConsole output) : TypeMessageWriter<Exception>
{
    /// <summary>
    ///     Writes an exception message to the configured output.
    /// </summary>
    /// <param name="message">The exception to be written. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor that can be used to format the message. Not used in this implementation.</param>
    public override void Write(Exception? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        output.WriteException(message ?? new Exception("No exception to display."));
    }
}
