namespace Ploch.CommandLine.Spectre.Output;

public class StringMessageFormatter : TypeMessageFormatter<string>
{
    public override string GetMessage(string message, IMessageFormatterProcessor? formatterProcessor = null) => message;
}
