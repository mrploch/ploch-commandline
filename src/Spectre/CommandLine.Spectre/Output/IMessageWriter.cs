namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Defines a writer for outputting messages with optional formatting.
/// </summary>
public interface IMessageWriter : IMessageHandler
{
    /// <summary>
    ///     Gets a value indicating whether this writer ends its own output with a line terminator.
    /// </summary>
    /// <remarks>
    ///     <see cref="IOutput.WriteLine{TMessage}" /> appends a terminator to a message this writer handled unless
    ///     this is <see langword="true" />. The default is <see langword="false" />, which keeps
    ///     <see cref="IOutput.WriteLine{TMessage}" />'s "followed by a line break" contract intact for a writer that
    ///     renders inline. A writer that emits its own trailing newline -- one that writes a line per item, for
    ///     example -- must report <see langword="true" /> so that a blank line is not added after it.
    /// </remarks>
    bool WritesLineTerminator => false;

    /// <summary>
    ///     Writes a message to the output with optional formatting.
    /// </summary>
    /// <param name="message">The message to write. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor to apply custom formatting to the message.</param>
    /// <remarks>
    ///     A writer that emits its own trailing newline must report <see cref="WritesLineTerminator" /> as
    ///     <see langword="true" />, so that <see cref="IOutput.WriteLine{TMessage}" /> does not add a second one.
    /// </remarks>
    void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null);
}

/// <summary>
///     Defines a strongly-typed writer for outputting messages of a specific type with optional formatting.
/// </summary>
/// <typeparam name="TMessage">The type of message this writer can handle.</typeparam>
public interface IMessageWriter<in TMessage> : IMessageWriter where TMessage : allows ref struct
{
    /// <summary>
    ///     Gets the type of message this writer can handle.
    /// </summary>
    new Type MessageType => typeof(TMessage);

    /// <summary>
    ///     Determines whether this writer can handle the specified message.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>true if this writer can handle the message; otherwise, false.</returns>
    new bool CanHandle(object? message) => message is TMessage;

    /// <summary>
    ///     Writes a strongly-typed message to the output with optional formatting.
    /// </summary>
    /// <param name="message">The message to write. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor to apply custom formatting to the message.</param>
    /// <remarks>
    ///     A writer that emits its own trailing newline must report <see cref="IMessageWriter.WritesLineTerminator" />
    ///     as <see langword="true" />, so that <see cref="IOutput.WriteLine{TMessage}" /> does not add a second one.
    /// </remarks>
    void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);
}
