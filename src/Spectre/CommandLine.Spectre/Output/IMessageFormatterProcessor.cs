namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public interface IMessageFormatterProcessor
{
    FormattableString GetMessageText(FormattableString message, string? markupTag = null);

    string? GetMessageText<TMessage>(TMessage? message, string? markupTag = null);

    void WriteMessage<TMessage>(TMessage message);
}
