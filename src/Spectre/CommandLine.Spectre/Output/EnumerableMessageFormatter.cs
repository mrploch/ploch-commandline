using System.Collections;
using System.Text;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

public class EnumerableMessageFormatter<TEnumerable> : TypeMessageFormatter<TEnumerable>
    where TEnumerable : IEnumerable
{
    public override string GetMessage(TEnumerable? enumerable, IMessageFormatterProcessor? formatterProcessor = null)
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
