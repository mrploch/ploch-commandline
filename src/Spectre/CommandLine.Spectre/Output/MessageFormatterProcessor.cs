using System.Runtime.CompilerServices;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Processes and formats messages using registered formatters and writers.
/// </summary>
/// <remarks>
///     This class implements the <see cref="IMessageFormatterProcessor" /> interface to provide
///     message formatting and writing capabilities. It uses a collection of registered formatters
///     and writers to handle different message types.
/// </remarks>
public class MessageFormatterProcessor(IEnumerable<IMessageFormatter> formatters, IEnumerable<IMessageWriter> writers) : IMessageFormatterProcessor
{
    /// <summary>
    ///     Formats a message as a <see cref="FormattableString" /> with optional markup.
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
                                        .Select(object? (arg) =>
                                                {
                                                    if (arg is null)
                                                    {
                                                        return string.Empty;
                                                    }

                                                    var formatter = GetFormatter(arg);

                                                    // Without a formatter the original object is kept rather than stringified here, so that its
                                                    // format specifier still applies when the string is composed: pre-rendering it would leave
                                                    // "{0:N2}" applied to a string, which silently drops the specifier.
                                                    return formatter is null ? arg : formatter.GetMessage(arg, this);
                                                })
                                        .ToArray();

        if (markupTag is null)
        {
            return FormattableStringFactory.Create(message.Format, formattedArguments);
        }

        // The tag wraps the format, not the rendered text, so the arguments remain arguments. Spectre escapes the
        // interpolation holes of a FormattableString when it renders one, which stops a bracket in caller data from
        // being parsed as a markup tag. Flattening the message into the format string first would forfeit that.
        return FormattableStringFactory.Create($"[{markupTag}]{message.Format}[/]", formattedArguments);
    }

    /// <summary>
    ///     Formats a message of type <typeparamref name="TMessage" /> with optional markup.
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
        var text = messageFormatter is null ? message.ToString() : messageFormatter.GetMessage(message, this);

        if (markupTag is null)
        {
            return text;
        }

        // The caller asked for a decoration, not for their data to be parsed as markup, so the content is escaped
        // before the tag is applied. Text the caller passes to IOutput.Write directly is still treated as markup:
        // that is the contract of a markup output, and only the tag added here is this library's doing.
        return $"[{markupTag}]{Markup.Escape(text ?? string.Empty)}[/]";
    }

    /// <summary>
    ///     Writes a message of type <typeparamref name="TMessage" /> using the appropriate writer.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write. If null, no action is taken.</param>
    /// <returns>
    ///     The registered writer that rendered the message, or <see langword="null" /> if none could handle it.
    /// </returns>
    /// <remarks>
    ///     The writer is selected by the type of <paramref name="message" /> and is then given that same message,
    ///     together with this processor so it can format the message itself. Handing the writer the already-formatted
    ///     text instead would defeat the type-based selection: a writer registered for a type a <see cref="string" />
    ///     cannot be cast to — <see cref="Exception" />, for example — would fail the cast in
    ///     <see cref="TypeMessageWriter{TMessage}.Write(object,IMessageFormatterProcessor)" />.
    /// </remarks>
    public IMessageWriter? WriteMessage<TMessage>(TMessage? message)
    {
        if (message is null)
        {
            return null;
        }

        var writer = GetWriter(message);

        writer?.Write(message, this);

        return writer;
    }

    /// <summary>
    ///     Gets the appropriate formatter for the specified message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to get a formatter for.</param>
    /// <returns>An <see cref="IMessageFormatter" /> that can handle the message, or null if none is found.</returns>
    private IMessageFormatter? GetFormatter<TMessage>(TMessage? message)
    {
        return formatters.FirstOrDefault(messageFormatter => messageFormatter.CanHandle(message));
    }

    /// <summary>
    ///     Gets the appropriate writer for the specified message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to get a writer for.</param>
    /// <returns>An <see cref="IMessageWriter" /> that can handle the message, or null if none is found.</returns>
    private IMessageWriter? GetWriter<TMessage>(TMessage? message)
    {
        return writers.FirstOrDefault(messageWriter => messageWriter.CanHandle(message));
    }
}
