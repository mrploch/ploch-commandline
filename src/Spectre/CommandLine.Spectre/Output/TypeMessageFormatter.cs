namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Base class for formatters that turn a message of a specific type into its display string.
/// </summary>
/// <typeparam name="TMessage">The message type handled by this formatter.</typeparam>
public abstract class TypeMessageFormatter<TMessage> : TypeMessageHandler<TMessage>, IMessageFormatter<TMessage>
{
    /// <summary>
    ///     Gets a value indicating whether this handler writes the message itself rather than only formatting it.
    /// </summary>
    public virtual bool IsWriter => false;

    /// <summary>
    ///     Formats a message supplied as <see cref="object" />, casting it to <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    /// <returns>The formatted message.</returns>
    public string GetMessage(object? message, IMessageFormatterProcessor? formatterProcessor = null) => GetMessage((TMessage?)message, formatterProcessor);

    /// <summary>
    ///     Formats a message of type <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    /// <returns>The formatted message.</returns>
    public abstract string GetMessage(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);
}
