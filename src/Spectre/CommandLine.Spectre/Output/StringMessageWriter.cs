using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

public class StringMessageWriter(IAnsiConsole console) : TypeMessageWriter<string>
{
    public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        console.Write(message ?? string.Empty);
    }
}
