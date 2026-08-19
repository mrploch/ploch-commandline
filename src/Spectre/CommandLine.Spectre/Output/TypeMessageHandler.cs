namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Base class for message handlers that accept messages of a specific type.
/// </summary>
/// <typeparam name="TMessage">The message type handled by this handler.</typeparam>
public abstract class TypeMessageHandler<TMessage> : IMessageHandler
{
    /// <summary>
    ///     Gets the message type handled by this handler.
    /// </summary>
    public virtual Type MessageType => typeof(TMessage);

    /// <summary>
    ///     Determines whether the supplied message can be handled by this handler.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="message" /> is an instance of <see cref="MessageType" />;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public virtual bool CanHandle(object? message) => MessageType.IsInstanceOfType(message);
}
