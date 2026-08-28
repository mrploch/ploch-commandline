namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Represents a handler for processing console messages of a specific type.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    ///     Gets the type of message that this handler can process.
    /// </summary>
    /// <remarks>
    ///     This property is used to define the specific type of message that the handler is capable of processing.
    ///     Handlers implementing <see cref="IMessageHandler" /> should provide the corresponding message type.
    /// </remarks>
    Type MessageType { get; }

    /// <summary>
    ///     Determines if the handler can process the specified message.
    /// </summary>
    /// <param name="message">The message to be evaluated for processing. It can be null.</param>
    /// <returns><c>true</c> if the handler can process the specified message; otherwise, <c>false</c>.</returns>
    bool CanHandle(object? message);
}
