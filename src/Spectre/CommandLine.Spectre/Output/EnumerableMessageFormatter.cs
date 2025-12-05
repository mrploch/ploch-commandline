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
    ///     Optional processor that can be used to format individual items in the collection.
    ///     If provided, it will be used to convert each item to its string representation.
    /// </param>
    /// <returns>
    ///     A string containing each item of the collection on a separate line,
    ///     prefixed with a pointing finger emoji, or an empty string if the collection is null.
    /// </returns>
    public override string GetMessage(IEnumerable? enumerable, IMessageFormatterProcessor? formatterProcessor = null)
    {
        if (enumerable is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var item in enumerable)
        {
            sb.AppendLine(Emoji.Known.BackhandIndexPointingRight + " " + formatterProcessor?.GetMessageText(item));
        }

        return sb.ToString();
    }
}
