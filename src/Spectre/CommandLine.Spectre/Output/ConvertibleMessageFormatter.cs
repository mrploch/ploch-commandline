using System.Globalization;

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
    /// <returns>The message converted using the current culture, or an empty string when <paramref name="message" /> is <see langword="null" />.</returns>
    public override string GetMessage(IConvertible? message, IMessageFormatterProcessor? formatterProcessor = null) =>
        message?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
}
