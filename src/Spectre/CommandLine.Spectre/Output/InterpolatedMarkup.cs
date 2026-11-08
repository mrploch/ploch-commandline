using System.Globalization;
using Ploch.Common.ArgumentChecking;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Renders a <see cref="FormattableString" /> as markup in which the template is parsed and every hole is
///     escaped, whatever the runtime type of its value.
/// </summary>
/// <remarks>
///     Spectre.Console's <c>MarkupInterpolated</c> escapes only a hole whose value is a <see cref="string" />. A
///     value of any other type was formatted and its text parsed as markup, so a value object rendering as
///     <c>[Admin]</c>, a <see cref="System.Text.StringBuilder" /> containing <c>[1]</c> or a hole such as
///     <c>{42:[0]}</c> threw <c>Could not find color or style</c> or <c>Unbalanced markup stack</c>. This helper is
///     shared by <see cref="FormattableStringMessageWriter" /> and the interpolated members of
///     <see cref="AnsiConsoleMarkupOutput" /> so the two paths cannot drift apart.
///     <para>
///         Formatting stays with <see cref="string.Format(IFormatProvider, string, object[])" />, which parses the
///         composite template (escaped braces, alignment) and resolves each hole through an
///         <see cref="ICustomFormatter" />. The one installed here renders the value the way <c>string.Format</c>
///         would with the caller's provider - that provider's own custom formatter first, then
///         <see cref="IFormattable" /> with the format specifier, then <c>ToString()</c> - and escapes the result,
///         so a custom formatter still receives the original value rather than a pre-rendered string. Alignment pads
///         the escaped text, as Spectre does for a string hole, so a value containing brackets is padded one column
///         short per bracket.
///     </para>
/// </remarks>
internal static class InterpolatedMarkup
{
    /// <summary>
    ///     Formats <paramref name="value" /> with <paramref name="formatProvider" />, escaping the text of every hole.
    /// </summary>
    /// <param name="value">The interpolated string whose template is markup and whose holes are data.</param>
    /// <param name="formatProvider">The format provider to apply, or <see langword="null" /> for the current culture.</param>
    /// <returns>Markup that is safe to pass to <c>IAnsiConsole.Markup</c>, never <see langword="null" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public static string Render(FormattableString value, IFormatProvider? formatProvider)
    {
        value.NotNull();

        var provider = formatProvider ?? CultureInfo.CurrentCulture;

        return string.Format(new EscapingFormatProvider(provider), value.Format, value.GetArguments());
    }

    /// <summary>
    ///     A provider that answers <c>string.Format</c>'s request for an <see cref="ICustomFormatter" /> with itself
    ///     and delegates everything else to the caller's provider.
    /// </summary>
    /// <param name="provider">The caller's provider, which the holes are formatted with.</param>
    private sealed class EscapingFormatProvider(IFormatProvider provider) : IFormatProvider, ICustomFormatter
    {
        // Resolved once per formatting operation, as string.Format itself does, so a provider that counts or varies
        // its answers is asked the same number of times it would be without this wrapper.
        private readonly ICustomFormatter? _customFormatter = provider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;

        /// <inheritdoc />
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : provider.GetFormat(formatType);

        /// <inheritdoc />
        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => Markup.Escape(RenderHole(format, arg));

        /// <summary>
        ///     Renders a hole with the same precedence <c>string.Format</c> applies: the caller's custom formatter
        ///     when it returns text, otherwise the value's <see cref="IFormattable" /> rendering, otherwise its
        ///     <c>ToString()</c>. The caller's provider, never this wrapper, is what the value sees, so a hole is
        ///     escaped exactly once; every branch is coalesced because <c>Markup.Escape</c> rejects a null.
        /// </summary>
        /// <param name="format">The hole's format specifier, or <see langword="null" /> when it has none.</param>
        /// <param name="arg">The hole's value.</param>
        /// <returns>The hole's text before escaping, never <see langword="null" />.</returns>
        private string RenderHole(string? format, object? arg)
        {
            var custom = _customFormatter?.Format(format, arg, provider);
            if (custom is not null)
            {
                return custom;
            }

            if (arg is IFormattable formattable)
            {
                return formattable.ToString(format, provider) ?? string.Empty;
            }

            return arg?.ToString() ?? string.Empty;
        }
    }
}
