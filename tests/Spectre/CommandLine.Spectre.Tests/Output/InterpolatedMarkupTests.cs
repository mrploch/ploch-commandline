using System.Globalization;
using System.Text;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="InterpolatedMarkup" />, the helper behind <c>IOutput.MarkupInterpolated</c> and
///     <c>FormattableStringMessageWriter</c>. The template of an interpolated string is markup and every hole is data:
///     the hole's formatted text is escaped whatever its runtime type, the format specifier, alignment and the
///     caller's provider (its <see cref="ICustomFormatter" /> included) are honoured the way <c>string.Format</c>
///     honours them, and a null never reaches <c>Markup.Escape</c>.
/// </summary>
public class InterpolatedMarkupTests
{
    [Fact]
    public void Render_should_escape_a_string_hole()
    {
        var value = "[red]";

        var markup = InterpolatedMarkup.Render($"value: {value}", CultureInfo.InvariantCulture);

        markup.Should().Be("value: [[red]]");
    }

    /// <summary>
    ///     Spectre.Console escapes only a hole whose runtime value is a <see cref="string" />; a value object whose
    ///     text carried a bracket was parsed as markup and threw (PLO-847).
    /// </summary>
    [Fact]
    public void Render_should_escape_a_non_string_hole()
    {
        var holder = new LiteralText("[Admin]");

        var markup = InterpolatedMarkup.Render($"role: {holder}", CultureInfo.InvariantCulture);

        markup.Should().Be("role: [[Admin]]");
    }

    [Fact]
    public void Render_should_escape_a_StringBuilder_hole()
    {
        var builder = new StringBuilder("[1]");

        var markup = InterpolatedMarkup.Render($"item {builder}", CultureInfo.InvariantCulture);

        markup.Should().Be("item [[1]]");
    }

    [Fact]
    public void Render_should_escape_brackets_produced_by_a_format_specifier()
    {
        var markup = InterpolatedMarkup.Render($"{42:[0]}", CultureInfo.InvariantCulture);

        markup.Should().Be("[[42]]");
    }

    [Fact]
    public void Render_should_leave_the_template_unescaped()
    {
        var value = "x";

        var markup = InterpolatedMarkup.Render($"[bold]{value}[/]", CultureInfo.InvariantCulture);

        markup.Should().Be("[bold]x[/]", "the literal parts are the caller's markup");
    }

    [Fact]
    public void Render_should_render_escaped_braces_in_the_template()
    {
        var markup = InterpolatedMarkup.Render($"{{x}} = {1}", CultureInfo.InvariantCulture);

        markup.Should().Be("{x} = 1", "composite formatting still resolves doubled braces");
    }

    [Fact]
    public void Render_should_apply_the_format_specifier_with_the_supplied_provider()
    {
        var markup = InterpolatedMarkup.Render($"{1234.5:N2}", CultureInfo.GetCultureInfo("de-DE"));

        markup.Should().Be("1.234,50");
    }

    /// <summary>
    ///     Pinned to a culture whose number format differs from the invariant one, so a regression to invariant
    ///     formatting cannot pass by coincidence on an en-US or invariant test runner. <c>CurrentCulture</c> is
    ///     per-thread, so setting it here does not leak into tests running in parallel.
    /// </summary>
    [Fact]
    public void Render_should_use_the_current_culture_when_no_provider_is_supplied()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var markup = InterpolatedMarkup.Render($"{1234.5:N2}", formatProvider: null);

            markup.Should().Be("1.234,50");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     A provider can carry an <see cref="ICustomFormatter" />, which <c>string.Format</c> consults for every hole
    ///     with the original argument. Escaping must not turn that argument into a string before the formatter sees it.
    /// </summary>
    [Fact]
    public void Render_should_hand_the_original_argument_to_a_custom_formatter_on_the_provider()
    {
        var markup = InterpolatedMarkup.Render($"n = {42}", new AngleBracketingNumberProvider());

        markup.Should().Be("n = <42>");
    }

    [Fact]
    public void Render_should_hand_a_string_argument_to_the_custom_formatter_before_escaping_it()
    {
        var provider = new RecordingProvider(text: null);
        var value = "[red]";

        InterpolatedMarkup.Render($"{value}", provider);

        provider.Arguments.Should().ContainSingle().Which.Should().Be("[red]", "the formatter sees the caller's value, not the escaped text");
    }

    [Fact]
    public void Render_should_escape_the_text_a_custom_formatter_returns()
    {
        var markup = InterpolatedMarkup.Render($"{1}", new RecordingProvider(text: "[red]x[/]"));

        markup.Should().Be("[[red]]x[[/]]", "a formatter's output is hole data, not markup");
    }

    [Fact]
    public void Render_should_fall_back_to_IFormattable_with_the_provider_when_the_custom_formatter_returns_null()
    {
        var provider = new RecordingProvider(text: null, culture: CultureInfo.GetCultureInfo("de-DE"));

        var markup = InterpolatedMarkup.Render($"{1234.5:N2}", provider);

        markup.Should().Be("1.234,50", "string.Format falls through to IFormattable with the same provider and specifier");
    }

    [Fact]
    public void Render_should_not_fall_back_when_the_custom_formatter_returns_an_empty_string()
    {
        var markup = InterpolatedMarkup.Render($"<{1234.5:N2}>", new RecordingProvider(text: string.Empty));

        markup.Should().Be("<>", "an empty answer is an answer, as it is for string.Format");
    }

    [Fact]
    public void Render_should_ask_the_provider_for_its_custom_formatter_once_per_call()
    {
        var provider = new RecordingProvider(text: null);

        InterpolatedMarkup.Render($"{1} and {2}", provider);

        provider.CustomFormatterLookups.Should().Be(1, "string.Format resolves the custom formatter once per operation");
    }

    [Fact]
    public void Render_should_render_a_null_hole_as_empty()
    {
        object? nothing = null;

        var markup = InterpolatedMarkup.Render($"a{nothing}b", CultureInfo.InvariantCulture);

        markup.Should().Be("ab");
    }

    [Fact]
    public void Render_should_render_a_null_returning_IFormattable_as_empty()
    {
        var markup = InterpolatedMarkup.Render($"a{new NullReturningFormattable()}b", CultureInfo.InvariantCulture);

        markup.Should().Be("ab", "the framework permits a null ToString, and Markup.Escape rejects one");
    }

    [Fact]
    public void Render_should_render_a_null_returning_ToString_as_empty()
    {
        var markup = InterpolatedMarkup.Render($"a{new NullReturningText()}b", CultureInfo.InvariantCulture);

        markup.Should().Be("ab");
    }

    [Fact]
    public void Render_should_pad_an_aligned_hole()
    {
        var markup = InterpolatedMarkup.Render($"{"ab",5}|{"cd",-4}|", CultureInfo.InvariantCulture);

        markup.Should().Be("   ab|cd  |", "alignment is still string.Format's job");
    }

    /// <summary>
    ///     Pins the accepted limitation, which Spectre.Console shares for a string hole: alignment pads the escaped
    ///     text, so a value containing brackets comes out one column short per bracket once the markup is rendered.
    /// </summary>
    [Fact]
    public void Render_should_pad_an_aligned_hole_on_its_escaped_text()
    {
        var markup = InterpolatedMarkup.Render($"{"[a]",6}|", CultureInfo.InvariantCulture);

        markup.Should().Be(" [[a]]|");
    }

    [Fact]
    public void Render_should_throw_ArgumentNullException_for_a_null_value()
    {
        var render = () => InterpolatedMarkup.Render(null!, CultureInfo.InvariantCulture);

        render.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void Render_should_return_a_template_without_holes_unchanged()
    {
        var markup = InterpolatedMarkup.Render($"[green]pure markup[/]", CultureInfo.InvariantCulture);

        markup.Should().Be("[green]pure markup[/]");
    }

    [Fact]
    public void Render_should_pad_an_aligned_non_string_hole_on_its_escaped_text_like_a_string_hole()
    {
        var holder = new LiteralText("[a]");

        var markup = InterpolatedMarkup.Render($"{holder,6}|", CultureInfo.InvariantCulture);

        markup.Should().Be(" [[a]]|", "a non-string hole follows the same alignment rule as a string one");
    }

    [Fact]
    public void Render_should_pad_an_aligned_null_hole_with_spaces()
    {
        object? nothing = null;

        var markup = InterpolatedMarkup.Render($"|{nothing,5}|", CultureInfo.InvariantCulture);

        markup.Should().Be("|     |");
    }

    [Fact]
    public void Render_should_keep_braces_inside_a_hole_verbatim()
    {
        var value = "{x}";

        var markup = InterpolatedMarkup.Render($"data: {value}", CultureInfo.InvariantCulture);

        markup.Should().Be("data: {x}", "a brace in a value is data, not a format token");
    }

    [Fact]
    public void Render_should_escape_the_whole_text_of_a_nested_FormattableString()
    {
        FormattableString inner = $"[x]{1}";

        var markup = InterpolatedMarkup.Render($"{inner}", CultureInfo.InvariantCulture);

        markup.Should().Be("[[x]]1", "a nested interpolated string is one hole: its template is data here, not markup");
    }

    /// <summary>A value whose text is fixed, standing in for any non-string hole such as a value object.</summary>
    private sealed class LiteralText(string text)
    {
        public override string ToString() => text;
    }

    /// <summary>A value whose <c>ToString()</c> returns null, which the framework permits.</summary>
    private sealed class NullReturningText
    {
        public override string? ToString() => null;
    }

    /// <summary>An IFormattable whose ToString deliberately returns null, which the framework permits.</summary>
    private sealed class NullReturningFormattable : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => null!;
    }

    /// <summary>A provider whose custom formatter wraps every integer in angle brackets and leaves everything else to the invariant culture.</summary>
    private sealed class AngleBracketingNumberProvider : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo.InvariantCulture.GetFormat(formatType);

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) =>
            arg is int number ? $"<{number}>" : Convert.ToString(arg, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    ///     A provider that records every argument its custom formatter is handed and how often the formatter was
    ///     looked up, and answers every hole with one fixed text (null to decline, as the contract allows).
    /// </summary>
    private sealed class RecordingProvider(string? text, CultureInfo? culture = null) : IFormatProvider, ICustomFormatter
    {
        private readonly CultureInfo _culture = culture ?? CultureInfo.InvariantCulture;

        public List<object?> Arguments { get; } = [];

        public int CustomFormatterLookups { get; private set; }

        public object? GetFormat(Type? formatType)
        {
            if (formatType != typeof(ICustomFormatter))
            {
                return _culture.GetFormat(formatType);
            }

            CustomFormatterLookups++;

            return this;
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            Arguments.Add(arg);

            // The interface is annotated non-nullable, but string.Format treats a null answer as "not handled" and
            // falls through, so a test provider has to be able to return one.
            return text!;
        }
    }
}
