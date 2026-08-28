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
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    /// <returns>
    ///     The message converted using <paramref name="formatProvider" />, or the current culture when it is
    ///     <see langword="null" />; an empty string when <paramref name="message" /> is <see langword="null" />.
    /// </returns>
    public override string GetMessage(IConvertible? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
        message?.ToString(formatProvider ?? CultureInfo.CurrentCulture) ?? string.Empty;
}
