using System.Collections;

namespace Ploch.CommandLine.Spectre.Output;

public class EnumerableMessageWriter(IOutput output) : TypeMessageWriter<IEnumerable>
{
    public override void Write(IEnumerable? enumerable, IMessageFormatterProcessor? formatterProcessor = null)
    {
        foreach (var item in enumerable)
        {
            output.WriteLine(formatterProcessor.GetMessageText(item));
        }
    }
}
