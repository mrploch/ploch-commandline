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
///         composite template (escaped braces, alignment). Every argument is wrapped in an <see cref="IFormattable" />
///         of this class's own, which is what <c>string.Format</c> calls for the hole; the wrapper then renders the
///         original value the way <c>string.Format</c> would with the caller's provider - that provider's own custom
///         formatter first, then the value's <see cref="IFormattable" /> with the format specifier, then
///         <c>ToString()</c> - and escapes the result, so a custom formatter still receives the original value
///         rather than a pre-rendered string. Alignment pads
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

        var scope = new HoleFormattingScope(formatProvider ?? CultureInfo.CurrentCulture);
        var holes = Array.ConvertAll(value.GetArguments(), static object (argument) => new EscapedHole(argument));

        return string.Format(scope, value.Format, holes);
    }

    /// <summary>
    ///     The provider handed to <c>string.Format</c>. It declines to supply an <see cref="ICustomFormatter" />, so
    ///     <c>string.Format</c> formats every hole through <see cref="EscapedHole" />'s <see cref="IFormattable" />;
    ///     it delegates every other request to the caller's provider, and carries that provider and its own custom
    ///     formatter for the holes to read back.
    /// </summary>
    /// <param name="provider">The caller's provider, which every hole is formatted with.</param>
    private sealed class HoleFormattingScope(IFormatProvider provider) : IFormatProvider
    {
        /// <summary>Gets the caller's provider, which every hole is formatted with.</summary>
        public IFormatProvider Provider => provider;

        /// <summary>
        ///     Gets the caller's own custom formatter, or <see langword="null" /> when the provider has none. Resolved
        ///     once per formatting operation, as <c>string.Format</c> itself does, so a provider that counts or varies
        ///     its answers is asked the same number of times it would be without this wrapper.
        /// </summary>
        public ICustomFormatter? CustomFormatter { get; } = provider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;

        /// <inheritdoc />
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? null : provider.GetFormat(formatType);
    }

    /// <summary>
    ///     A hole's value, rendered the way <c>string.Format</c> would render it with the caller's provider and
    ///     escaped, so that its text is printed rather than parsed as markup.
    /// </summary>
    /// <param name="value">The hole's original value, which the caller's custom formatter receives unchanged.</param>
    private sealed class EscapedHole(object? value) : IFormattable
    {
        /// <summary>
        ///     Formats the value with the same precedence <c>string.Format</c> applies - the caller's custom formatter
        ///     when it returns text, otherwise the value's <see cref="IFormattable" /> rendering, otherwise its
        ///     <c>ToString()</c> - and escapes the result.
        /// </summary>
        /// <param name="format">The hole's format specifier, or <see langword="null" /> when it has none.</param>
        /// <param name="formatProvider">
        ///     The provider in effect for the composite operation: the <see cref="HoleFormattingScope" /> when called
        ///     by <see cref="Render" />, from which the caller's provider is taken; any other provider is used as it is.
        /// </param>
        /// <returns>The escaped text of the hole, never <see langword="null" />.</returns>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var (provider, customFormatter) = formatProvider is HoleFormattingScope scope
                                                  ? (scope.Provider, scope.CustomFormatter)
                                                  : (formatProvider ?? CultureInfo.CurrentCulture, (ICustomFormatter?)null);

            // The caller's provider, never the scope, is what the value sees, so a hole is escaped exactly once. The
            // text is coalesced because a null value, or an IFormattable or ToString() that returns null (which the
            // framework permits whatever the annotations say), renders as nothing, and Markup.Escape rejects a null.
            return Markup.Escape(RenderText(format, provider, customFormatter) ?? string.Empty);
        }

        /// <inheritdoc />
        public override string ToString() => ToString(format: null, formatProvider: null);

        private string? RenderText(string? format, IFormatProvider provider, ICustomFormatter? customFormatter) =>
            customFormatter?.Format(format, value, provider)
            ?? (value is IFormattable formattable ? formattable.ToString(format, provider) : value?.ToString());
    }
}
