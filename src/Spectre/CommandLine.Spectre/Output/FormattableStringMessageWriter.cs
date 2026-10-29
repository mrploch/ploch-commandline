using System.Globalization;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Renders a <see cref="FormattableString" /> as interpolated markup: the template is parsed as Spectre markup,
///     the <see cref="string" /> values in the holes are escaped rather than parsed (a value of another type is
///     formatted and its text parsed as markup, as Spectre.Console does), and the holes are formatted with the
///     caller's format provider.
/// </summary>
/// <param name="output">The console the messages are written to.</param>
/// <remarks>
///     This is the writer <see cref="IOutput.Write{TMessage}" /> selects for a <see cref="FormattableString" />, so
///     a consumer that registers its own <see cref="IMessageWriter{TMessage}" /> for that type through
///     <see cref="ServiceCollectionOutputExtensions.AddMessageWriter{TMessageType,TWriter}" /> replaces it. The
///     holes are deliberately not run through the registered <see cref="IMessageFormatter" /> instances: the
///     formatters pre-render each argument to a string, which drops a format specifier such as <c>{total:N2}</c>,
///     hides an argument from an <see cref="ICustomFormatter" /> supplied by the provider, and replaces an
///     exception's <c>ToString()</c> with the formatter's text. A writer that wants that behaviour can call
///     <see cref="IMessageFormatterProcessor.GetMessageText(FormattableString,string,IFormatProvider)" /> itself.
/// </remarks>
public class FormattableStringMessageWriter(IAnsiConsole output) : TypeMessageWriter<FormattableString>
{
    /// <summary>
    ///     Writes a formattable string message to the output as interpolated markup.
    /// </summary>
    /// <param name="message">The formattable string message to write. Can be null, in which case nothing is written.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages. Not used by this writer; see the class remarks.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    public override void Write(FormattableString? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
    {
        if (message is null)
        {
            return;
        }

        // Rendering the interpolated string is where its holes are formatted, so the provider has to be applied
        // here; MarkupInterpolated escapes the string holes, which is what keeps a '[' in caller data from being parsed
        // (a non-string hole is formatted and parsed, which is why the summary tells callers to convert such values).
        output.MarkupInterpolated(formatProvider ?? CultureInfo.CurrentCulture, message);
    }
}
