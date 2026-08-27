namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Base class for writers that render a message of a specific type to the console.
/// </summary>
/// <typeparam name="TMessage">The message type written by this writer.</typeparam>
public abstract class TypeMessageWriter<TMessage> : TypeMessageHandler<TMessage>, IMessageWriter<TMessage>
{
    /// <summary>
    ///     Gets a value indicating whether this writer ends its output with a line terminator.
    /// </summary>
    /// <remarks>
    ///     Declared here, on the type that implements <see cref="IMessageWriter{TMessage}" />, rather than left to the
    ///     interface's default implementation. Interface mapping is fixed at this class, so a property introduced by a
    ///     derived writer would not map to the interface member and a caller holding an <see cref="IMessageWriter" />
    ///     would still see the default. Overriding a virtual declared here maps correctly.
    /// </remarks>
    public virtual bool WritesLineTerminator => false;

    /// <summary>
    ///     Writes a message of type <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    public abstract void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null);

    /// <summary>
    ///     Writes a message supplied as <see cref="object" />, casting it to <typeparamref name="TMessage" />.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages, if any.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    public void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
        Write((TMessage?)message, formatterProcessor, formatProvider);
}
