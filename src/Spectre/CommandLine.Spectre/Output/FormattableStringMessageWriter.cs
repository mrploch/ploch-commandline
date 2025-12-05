using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     A message writer that handles <see cref="FormattableString" /> objects and writes them to the output.
/// </summary>
/// <param name="output">The output destination where messages will be written.</param>
public class FormattableStringMessageWriter(IAnsiConsole output) : TypeMessageWriter<FormattableString>
{
    /// <summary>
    ///     Writes a formattable string message to the output.
    /// </summary>
    /// <param name="message">The formattable string message to write. Can be null.</param>
    /// <param name="formatterProcessor">Optional processor that can format the message before writing. If null, the message is written as-is.</param>
    /// <remarks>
    ///     If both the formatted message and the original message are null, nothing will be written to the output.
    /// </remarks>
    public override void Write(FormattableString? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        var messageText = formatterProcessor?.GetMessageText(message) ?? message;

        if (messageText is null)
        {
            return;
        }

        output.MarkupInterpolated(messageText);
    }
}
