using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="FormattableStringMessageWriter" />, the writer <c>IOutput.Write</c> selects for an
///     interpolated string. It renders the message as interpolated markup with the caller's provider, leaves the
///     holes to <c>string.Format</c> rather than the registered formatters, and stays silent when there is nothing
///     to write.
/// </summary>
public class FormattableStringMessageWriterTests
{
    /// <summary>
    ///     The writer used to pre-render each hole through the matching formatter. That dropped a format specifier
    ///     such as <c>{total:N2}</c> (the formatter returned a string, and N2 applied to a string is ignored), hid the
    ///     argument from an <c>ICustomFormatter</c> on the provider, and replaced an exception's <c>ToString()</c>
    ///     with the formatter's text. Now that <c>IOutput.Write</c> reaches this writer, the holes must render as
    ///     they did on the direct path.
    /// </summary>
    [Fact]
    public void Write_should_not_run_the_holes_through_the_supplied_processor()
    {
        using var console = new RecordingConsole();
        var processor = new MessageFormatterProcessor([new BracketingFormatter()], []);

        new FormattableStringMessageWriter(console.Console).Write($"value: {"x"}", processor, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("value: x", "the formatters are not applied to interpolation holes");
    }

    [Fact]
    public void Write_should_keep_the_format_specifier_of_a_hole_when_a_formatter_matches_the_argument()
    {
        using var console = new RecordingConsole();
        var processor = new MessageFormatterProcessor([new ConvertibleMessageFormatter()], []);

        new FormattableStringMessageWriter(console.Console).Write($"total: {1234.5:N2}", processor, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("total: 1,234.50", "a pre-rendered argument would have lost the N2 specifier");
    }

    /// <summary>
    ///     A provider can carry an <see cref="ICustomFormatter" />, which <c>string.Format</c> consults for every hole.
    ///     A pre-rendered hole is a string by the time it gets there, so the custom formatter never saw the number.
    /// </summary>
    [Fact]
    public void Write_should_let_a_custom_formatter_on_the_provider_see_the_original_argument()
    {
        using var console = new RecordingConsole();
        var processor = new MessageFormatterProcessor([new ConvertibleMessageFormatter()], []);

        new FormattableStringMessageWriter(console.Console).Write($"n = {42}", processor, new AngleBracketingNumberProvider());

        console.Output.Should().Be("n = <42>", "the custom formatter must receive the int, not a pre-rendered string");
    }

    [Fact]
    public void WritesLineTerminator_should_be_false_so_WriteLine_adds_the_terminator()
    {
        using var console = new RecordingConsole();

        IMessageWriter writer = new FormattableStringMessageWriter(console.Console);

        writer.WritesLineTerminator.Should().BeFalse();
    }

    [Fact]
    public void Write_should_render_the_raw_message_when_no_processor_is_supplied()
    {
        using var console = new RecordingConsole();

        new FormattableStringMessageWriter(console.Console).Write($"value: {"x"}", formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("value: x");
    }

    [Fact]
    public void Write_should_write_nothing_when_the_message_is_null_and_no_processor_is_supplied()
    {
        using var console = new RecordingConsole();

        new FormattableStringMessageWriter(console.Console).Write(null, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().BeEmpty();
    }

    /// <summary>
    ///     Rendering the interpolated string is where its holes are formatted, so a provider that reaches the writer
    ///     but is not applied at that point is silently discarded - the writer honoured it for nested formatters and
    ///     then formatted every remaining hole with the ambient culture.
    /// </summary>
    [Fact]
    public void Write_should_format_the_interpolated_holes_with_the_supplied_provider()
    {
        using var console = new RecordingConsole();

        new FormattableStringMessageWriter(console.Console).Write($"value: {1234.5}", formatProvider: CultureInfo.GetCultureInfo("de-DE"));

        console.Output.Should().Be("value: 1234,5", "de-DE uses a comma as the decimal separator");
    }

    [Fact]
    public void Write_should_escape_markup_carried_by_the_interpolated_arguments()
    {
        using var console = new RecordingConsole();

        new FormattableStringMessageWriter(console.Console).Write($"{"[red]"}", formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("[red]", "an argument value must never be reinterpreted as markup");
    }

    /// <summary>A provider whose custom formatter wraps every integer in angle brackets and leaves everything else to the invariant culture.</summary>
    private sealed class AngleBracketingNumberProvider : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo.InvariantCulture.GetFormat(formatType);

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) =>
            arg is int number ? $"<{number}>" : Convert.ToString(arg, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private sealed class BracketingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) => $"<{message}>";
    }
}
