using System.Runtime.CompilerServices;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
/// Processes and formats messages using registered formatters and writers.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IMessageFormatterProcessor"/> interface to provide
/// message formatting and writing capabilities. It uses a collection of registered formatters
/// and writers to handle different message types.
/// </remarks>
public class MessageFormatterProcessor(IEnumerable<IMessageFormatter> formatters, IEnumerable<IMessageWriter> writers) : IMessageFormatterProcessor
{
    /// <summary>
    /// Formats a message as a <see cref="FormattableString"/> with optional markup.
    /// </summary>
    /// <param name="message">The message to format. Can be null.</param>
    /// <param name="markupTag">Optional markup tag to apply to the message (e.g., "b" for bold, "i" for italic).</param>
    /// <returns>A formatted string with applied markup, or an empty string if the input is null.</returns>
    public FormattableString GetMessageText(FormattableString? message, string? markupTag = null)
    {
        if (message is null)
        {
            return FormattableStringFactory.Create(string.Empty);
        }

        var formattedArguments = message.GetArguments()
                                        .Select(arg =>
                                                {
                                                    if (arg is null)
                                                    {
                                                        return string.Empty;
                                                    }

                                                    var formatter = GetFormatter(arg);

                                                    return formatter?.GetMessage(arg, this) ?? arg.ToString();
                                                })
                                        .ToArray();

        var formattedMessage = FormattableStringFactory.Create(message.Format, formattedArguments);

        return markupTag == null ? formattedMessage : FormattableStringFactory.Create($"[{markupTag}]{formattedMessage}[/]");
    }

    /// <summary>
    /// Formats a message of type <typeparamref name="TMessage"/> with optional markup.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to format.</typeparam>
    /// <param name="message">The message to format. Can be null.</param>
    /// <param name="markupTag">Optional markup tag to apply to the message (e.g., "b" for bold, "i" for italic).</param>
    /// <returns>A formatted string with applied markup, or an empty string if the input is null.</returns>
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

    /// <summary>
    /// Writes a message of type <typeparamref name="TMessage"/> using the appropriate writer.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write. If null, no action is taken.</param>
    public void WriteMessage<TMessage>(TMessage? message)
    {
        if (message is null)
        {
            return;
        }

        var writer = GetWriter(message);

        writer?.Write(GetMessageText(message));
    }

    /// <summary>
    /// Gets the appropriate formatter for the specified message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to get a formatter for.</param>
    /// <returns>An <see cref="IMessageFormatter"/> that can handle the message, or null if none is found.</returns>
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

    /// <summary>
    /// Gets the appropriate writer for the specified message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to get a writer for.</param>
    /// <returns>An <see cref="IMessageWriter"/> that can handle the message, or null if none is found.</returns>
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
