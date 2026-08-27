using System.Globalization;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Renders a message that no formatter or writer claimed, honouring the caller's format provider.
/// </summary>
/// <remarks>
///     Shared because the same fallback is needed wherever the pipeline runs out of registered handlers: the
///     output's own last resort, and the enumerable formatter and writer when no processor was supplied. A
///     parameterless <c>ToString()</c> would silently ignore the provider and format with the current culture,
///     which is the whole defect this helper exists to avoid.
/// </remarks>
internal static class FormattedText
{
    /// <summary>
    ///     Renders <paramref name="message" /> using <paramref name="formatProvider" /> when the message supports it.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to render.</typeparam>
    /// <param name="message">The message to render.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> for the current culture.</param>
    /// <returns>The rendered text, never <see langword="null" />.</returns>
    /// <remarks>
    ///     The result is coalesced because a custom <see cref="IFormattable" /> may return <see langword="null" />,
    ///     which would otherwise fail inside the console rather than rendering as empty output.
    /// </remarks>
    public static string Render<TMessage>(TMessage? message, IFormatProvider? formatProvider) =>
        message is IFormattable formattable
            ? formattable.ToString(format: null, formatProvider ?? CultureInfo.CurrentCulture) ?? string.Empty
            : message?.ToString() ?? string.Empty;
}
