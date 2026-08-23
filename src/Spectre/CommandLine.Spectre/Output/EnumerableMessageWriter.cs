using System.Collections;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     A message writer that handles writing enumerable collections to the output.
/// </summary>
/// <param name="output">The output interface used to write messages.</param>
public class EnumerableMessageWriter(IAnsiConsole output) : TypeMessageWriter<IEnumerable>
{
    /// <summary>
    ///     Gets a value indicating whether this writer ends its output with a line terminator.
    /// </summary>
    /// <remarks>Always <see langword="true" />: every item is written on its own line.</remarks>
    public override bool WritesLineTerminator => true;

    /// <summary>
    ///     Writes each item in the enumerable collection to the output.
    /// </summary>
    /// <param name="enumerable">The enumerable collection to write. If null, a "No items to display" message is shown.</param>
    /// <param name="formatterProcessor">
    ///     Optional formatter processor that can be used to format each item before writing.
    ///     If null, the item's default string representation is used.
    /// </param>
    public override void Write(IEnumerable? enumerable, IMessageFormatterProcessor? formatterProcessor = null)
    {
        if (enumerable is null)
        {
            output.WriteLine("No items to display.");

            return;
        }

        foreach (var item in enumerable)
        {
            output.WriteLine(formatterProcessor == null ? item.ToString()! : formatterProcessor.GetMessageText(item)!);
        }
    }
}
