namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
/// Defines a writer for outputting messages with optional formatting.
/// </summary>
public interface IMessageWriter : IMessageHandler
{
    /// <summary>
    /// Writes a message to the output with optional formatting.
    /// </summary>
    /// <param name="message">The message to write. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor to apply custom formatting to the message.</param>
    void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null);
}

/// <summary>
/// Defines a strongly-typed writer for outputting messages of a specific type with optional formatting.
/// </summary>
/// <typeparam name="TMessage">The type of message this writer can handle.</typeparam>
public interface IMessageWriter<in TMessage> : IMessageWriter
    where TMessage : allows ref struct
{
    /// <summary>
    /// Gets the type of message this writer can handle.
    /// </summary>
    new Type MessageType => typeof(TMessage);

    /// <summary>
    /// Determines whether this writer can handle the specified message.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>true if this writer can handle the message; otherwise, false.</returns>
    new bool CanHandle(object? message) => message is TMessage;

    /// <summary>
    /// Writes a strongly-typed message to the output with optional formatting.
    /// </summary>
    /// <param name="message">The message to write. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor to apply custom formatting to the message.</param>
    void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);
}
