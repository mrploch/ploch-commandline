namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Formats messages that implement the <see cref="IConvertible" /> interface.
/// </summary>
public class ConvertibleMessageFormatter : TypeMessageFormatter<IConvertible>
{
    /// <summary>
    ///     Formats an <see cref="IConvertible" /> message into a string representation.
    /// </summary>
    /// <param name="message">The message to format. Can be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor that can be used for additional formatting operations. Can be null.</param>
    /// <returns>A string representation of the provided message.</returns>
    public override string GetMessage(IConvertible? message, IMessageFormatterProcessor? formatterProcessor = null) => throw new NotImplementedException();
}
