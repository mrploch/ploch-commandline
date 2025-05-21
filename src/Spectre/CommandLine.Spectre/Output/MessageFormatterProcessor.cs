namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public class MessageFormatterProcessor(IEnumerable<IMessageFormatter> formatters, IEnumerable<IMessageWriter> writers) : IMessageFormatterProcessor
{
    public FormattableString GetMessageText(FormattableString message, string? markupTag = null) => throw new NotImplementedException();

    public string? GetMessageText<TMessage>(TMessage? message, string? markupTag = null)
    {
        if (message is null)
        {
            return string.Empty;
        }

        var messageFormatter = GetFormatter(message);
        if (messageFormatter == null)
        {
            return markupTag == null ? message.ToString() : $"[{markupTag}]{message}[/]";
        }

        return markupTag == null ? messageFormatter.GetMessage(message, this) : $"[{markupTag}]{messageFormatter.GetMessage(message, this)}[/]";
    }

    public void WriteMessage<TMessage>(TMessage? message)
    {
        if (message is null)
        {
            return;
        }

        var writer = GetWriter(message);

        writer?.Write(GetMessageText(message));
    }

    private IMessageFormatter? GetFormatter<TMessage>(TMessage? message)
    {
        foreach (var messageFormatter in formatters)
        {
            if (messageFormatter.CanHandle(message))
            {
                return messageFormatter;
            }
        }

        return null;
    }

    private IMessageWriter? GetWriter<TMessage>(TMessage? message)
    {
        foreach (var messageWriter in writers)
        {
            if (messageWriter.CanHandle(message))
            {
                return messageWriter;
            }
        }

        return null;
    }
}
