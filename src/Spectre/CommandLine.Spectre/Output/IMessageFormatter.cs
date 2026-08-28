namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Defines a contract for formatting messages into a string representation.
/// </summary>
public interface IMessageFormatter : IMessageHandler
{
    /// <summary>
    ///     Formats a message object into its string representation.
    /// </summary>
    /// <param name="message">The message object to format. Can be null.</param>
    /// <param name="formatterProcessor">The processor that provides formatting capabilities.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    /// <returns>A string representation of the provided message.</returns>
    string GetMessage(object? message, IMessageFormatterProcessor formatterProcessor, IFormatProvider? formatProvider = null);
}

/// <summary>
///     Defines a contract for formatting messages of a specific type into a string representation.
/// </summary>
/// <typeparam name="TMessage">The type of message to format.</typeparam>
public interface IMessageFormatter<in TMessage> : IMessageFormatter
{
    /// <summary>
    ///     Formats a message object into its string representation.
    /// </summary>
    /// <param name="message">The message object to format. Can be null.</param>
    /// <param name="formatterProcessor">The processor that provides formatting capabilities. Can be null.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    /// <returns>A string representation of the provided message.</returns>
    new string GetMessage(object? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
        GetMessage((TMessage?)message, formatterProcessor, formatProvider);

    /// <summary>
    ///     Formats a strongly-typed message into its string representation.
    /// </summary>
    /// <param name="message">The strongly-typed message to format. Can be null.</param>
    /// <param name="formatterProcessor">The processor that provides formatting capabilities. Can be null.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    /// <returns>A string representation of the provided message.</returns>
    string GetMessage(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null);
}
