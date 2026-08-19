namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Formats <see cref="string" /> messages, substituting an empty string for <see langword="null" />.
/// </summary>
public class StringMessageFormatter : TypeMessageFormatter<string>
{
    /// <summary>
    ///     Returns the message unchanged, or an empty string when it is <see langword="null" />.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages. Not used by this formatter.</param>
    /// <returns>The message, or <see cref="string.Empty" /> when <paramref name="message" /> is <see langword="null" />.</returns>
    public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null) => message ?? string.Empty;
}
