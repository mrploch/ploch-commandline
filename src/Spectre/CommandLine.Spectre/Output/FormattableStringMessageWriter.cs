using System.Globalization;
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
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    /// <remarks>
    ///     If both the formatted message and the original message are null, nothing will be written to the output.
    /// </remarks>
    public override void Write(FormattableString? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
    {
        var messageText = formatterProcessor?.GetMessageText(message, formatProvider: formatProvider) ?? message;

        if (messageText is null)
        {
            return;
        }

        // Rendering the interpolated string is where its holes are formatted, so the provider has to be applied
        // here. Without it the writer honoured the provider for nested formatters but then formatted every
        // remaining hole with the ambient culture.
        output.MarkupInterpolated(formatProvider ?? CultureInfo.CurrentCulture, messageText);
    }
}
