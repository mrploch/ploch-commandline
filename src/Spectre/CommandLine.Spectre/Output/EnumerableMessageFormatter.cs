using System.Collections;
using System.Text;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Formats IEnumerable objects into a string representation with each item on a new line prefixed with a pointing finger emoji.
/// </summary>
public class EnumerableMessageFormatter : TypeMessageFormatter<IEnumerable>
{
    /// <summary>
    ///     Converts an IEnumerable collection into a formatted string representation.
    ///     Each item in the collection is displayed on a new line with a pointing finger emoji prefix.
    /// </summary>
    /// <param name="enumerable">The IEnumerable collection to format. If null, an empty string is returned.</param>
    /// <param name="formatterProcessor">
    ///     Optional processor used to format individual items in the collection. When it is omitted, each item is
    ///     rendered with its own <see cref="object.ToString" />.
    /// </param>
    /// <param name="formatProvider">
    ///     The format provider applied to each item, or <see langword="null" /> to use the current culture.
    /// </param>
    /// <returns>
    ///     A string containing each item of the collection on a separate line,
    ///     prefixed with a pointing finger emoji, or an empty string if the collection is null.
    /// </returns>
    public override string GetMessage(IEnumerable? enumerable, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
    {
        if (enumerable is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var item in enumerable)
        {
            // Only the absence of a processor falls back to the item's own ToString. A processor that returns
            // null is answering, not abstaining: GetMessageText is declared string?, so a formatter may return null
            // to suppress an item, and coalescing that to ToString() would print the very text it withheld.
            // Previously the null-conditional call alone yielded null whenever no processor was supplied, so every
            // line rendered as a bare emoji and the item text was dropped - contradicting the optional parameter.
            var text = formatterProcessor is null
                           ? FormattedText.Render(item, formatProvider)
                           : formatterProcessor.GetMessageText(item, formatProvider: formatProvider);

            sb.Append(Emoji.Known.BackhandIndexPointingRight).Append(' ').AppendLine(text ?? string.Empty);
        }

        return sb.ToString();
    }
}
