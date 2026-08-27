using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Writes <see cref="string" /> messages directly to the console.
/// </summary>
/// <param name="console">The console the messages are written to.</param>
public class StringMessageWriter(IAnsiConsole console) : TypeMessageWriter<string>
{
    /// <summary>
    ///     Writes the message to the console, substituting an empty string for <see langword="null" />.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="formatterProcessor">The processor used to format nested messages. Not used by this writer.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> to use the current culture.</param>
    public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
    {
        console.Write(message ?? string.Empty);
    }
}
