using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="FormattableStringMessageWriter" />, which has to fall back to the raw message when no
///     processor is supplied and stay silent when there is nothing to write.
/// </summary>
public class FormattableStringMessageWriterTests
{
    [Fact]
    public void Write_should_render_the_message_through_the_supplied_processor()
    {
        using var console = new RecordingConsole();
        var processor = new MessageFormatterProcessor([new BracketingFormatter()], []);

        new FormattableStringMessageWriter(console.Console).Write($"value: {"x"}", processor, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("value: <x>");
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

    private sealed class BracketingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) => $"<{message}>";
    }
}
