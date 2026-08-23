namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Defines methods for processing and formatting messages.
///     This interface allows extracting and writing formatted representations
///     of messages, with optional markup tags applied.
/// </summary>
public interface IMessageFormatterProcessor
{
    /// <summary>
    ///     Formats a message as a <see cref="FormattableString" /> with optional markup.
    /// </summary>
    /// <param name="message">The message to format, or <c>null</c>.</param>
    /// <param name="markupTag">Optional markup tag to apply to the message (e.g., "b" for bold, "i" for italic).</param>
    /// <returns>A formatted string with applied markup, or an empty string if the input is null.</returns>
    FormattableString GetMessageText(FormattableString? message, string? markupTag = null);

    /// <summary>
    ///     Formats a message of type <typeparamref name="TMessage" /> with optional markup.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to format.</typeparam>
    /// <param name="message">The message to format, or <c>null</c>.</param>
    /// <param name="markupTag">Optional markup tag to apply to the message (e.g., "b" for bold, "i" for italic).</param>
    /// <returns>A formatted string with applied markup, or <c>null</c> if the input is null.</returns>
    string? GetMessageText<TMessage>(TMessage? message, string? markupTag = null);

    /// <summary>
    ///     Writes a message of type <typeparamref name="TMessage" /> to the output.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <returns>
    ///     <see langword="true" /> if a registered writer handled the message; otherwise <see langword="false" />,
    ///     indicating the caller should fall back to its own rendering.
    /// </returns>
    /// <remarks>
    ///     The writer is chosen by the type of <paramref name="message" /> and receives that message unchanged,
    ///     along with this processor, so that formatting stays the writer's responsibility.
    /// </remarks>
    bool WriteMessage<TMessage>(TMessage message);
}
