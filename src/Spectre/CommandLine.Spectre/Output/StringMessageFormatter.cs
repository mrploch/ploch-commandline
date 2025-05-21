namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public class StringMessageFormatter : TypeMessageFormatter<string>
{
    public override string GetMessage(string message, IMessageFormatterProcessor? formatterProcessor = null) => message;
}
