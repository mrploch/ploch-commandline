namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Base class for writers that render a message of a specific type to the console.
/// </summary>
/// <typeparam name="TMessage">The message type written by this writer.</typeparam>
public abstract class TypeMessageWriter<TMessage> : TypeMessageHandler<TMessage>, IMessageWriter<TMessage>
{
    /// <summary>
    ///     Writes a message of type <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    public abstract void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);

    /// <summary>
    ///     Writes a message supplied as <see cref="object" />, casting it to <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    public void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null) => Write((TMessage?)message, formatterProcessor);
}
