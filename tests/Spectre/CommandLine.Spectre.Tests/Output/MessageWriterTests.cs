using System.Collections;
using System.ComponentModel;
using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="EnumerableMessageWriter" />: it is the only writer that has to cope with a null
///     collection and with the absence of a formatter processor.
/// </summary>
public class EnumerableMessageWriterTests
{
    [Fact]
    public void Write_should_report_an_empty_collection_placeholder_when_the_collection_is_null()
    {
        using var console = new RecordingConsole();

        new EnumerableMessageWriter(console.Console).Write(null, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("No items to display." + Environment.NewLine);
    }

    [Fact]
    public void Write_should_write_one_line_per_item_using_ToString_when_no_processor_is_supplied()
    {
        using var console = new RecordingConsole();

        string[] items = ["alpha", "beta"];

        new EnumerableMessageWriter(console.Console).Write(items, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be($"alpha{Environment.NewLine}beta{Environment.NewLine}");
    }

    [Fact]
    public void Write_should_route_each_item_through_the_supplied_formatter_processor()
    {
        using var console = new RecordingConsole();
        var processor = new MessageFormatterProcessor([new BracketingFormatter()], []);

        string[] items = ["alpha"];

        new EnumerableMessageWriter(console.Console).Write(items, processor, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be($"<alpha>{Environment.NewLine}");
    }

    [Fact]
    public void Write_should_write_nothing_for_an_empty_collection()
    {
        using var console = new RecordingConsole();

        new EnumerableMessageWriter(console.Console).Write(Array.Empty<string>(), formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void CanHandle_should_accept_collections_and_reject_scalars()
    {
        using var console = new RecordingConsole();
        var writer = new EnumerableMessageWriter(console.Console);

        int[] items = [1];

        writer.CanHandle(items).Should().BeTrue();
        writer.CanHandle(1).Should().BeFalse();
        writer.MessageType.Should().Be<IEnumerable>();
    }

    private sealed class BracketingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) => $"<{message}>";
    }
}

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

/// <summary>
///     Cover for <see cref="ExceptionMessageWriter" />, including the substitute exception it renders when asked
///     to write nothing.
/// </summary>
public class ExceptionMessageWriterTests
{
    [Fact]
    public void Write_should_render_the_exception_message()
    {
        using var console = new RecordingConsole();

        new ExceptionMessageWriter(console.Console).Write(new InvalidOperationException("the failure detail"), formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Contain("the failure detail");
    }

    [Fact]
    public void Write_should_render_a_placeholder_exception_when_the_message_is_null()
    {
        using var console = new RecordingConsole();

        new ExceptionMessageWriter(console.Console).Write(null, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Contain("No exception to display.");
    }
}

/// <summary>
///     Cover for <see cref="StringMessageWriter" />, whose only real behaviour is the null substitution.
/// </summary>
public class StringMessageWriterTests
{
    [Fact]
    public void Write_should_write_the_message_verbatim()
    {
        using var console = new RecordingConsole();

        new StringMessageWriter(console.Console).Write("[not markup]", formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().Be("[not markup]", "this writer bypasses markup parsing");
    }

    [Fact]
    public void Write_should_write_nothing_for_a_null_message()
    {
        using var console = new RecordingConsole();

        new StringMessageWriter(console.Console).Write(null, formatProvider: CultureInfo.InvariantCulture);

        console.Output.Should().BeEmpty();
    }
}

/// <summary>
///     Cover for <see cref="StringMessageFormatter" />, whose only real behaviour is the null substitution.
/// </summary>
public class StringMessageFormatterTests
{
    [Fact]
    public void GetMessage_should_return_the_message_unchanged()
    {
        new StringMessageFormatter().GetMessage("value", formatProvider: CultureInfo.InvariantCulture).Should().Be("value");
    }

    [Fact]
    public void GetMessage_should_substitute_an_empty_string_for_a_null_message()
    {
        new StringMessageFormatter().GetMessage(null, formatProvider: CultureInfo.InvariantCulture).Should().BeEmpty();
    }
}

/// <summary>
///     Cover for <see cref="Win32ExceptionMessageFormatter" />, which adds the native error code to the rendered text.
/// </summary>
public class Win32ExceptionMessageFormatterTests
{
    [Fact]
    public void GetMessage_should_include_the_native_error_code()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        var result = formatter.GetMessage(new Win32Exception(5), formatProvider: CultureInfo.InvariantCulture);

        result.Should().Contain("<Error Code: 5>").And.Contain(nameof(Win32Exception));
    }

    [Fact]
    public void GetMessage_should_append_the_inner_exception_details_only_when_there_is_one()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        formatter.GetMessage(new Win32Exception(2, "no inner"), formatProvider: CultureInfo.InvariantCulture).Should().NotContain("Inner exception");
        formatter.GetMessage(new Win32Exception("outer", new InvalidOperationException("inner detail")), formatProvider: CultureInfo.InvariantCulture)
                 .Should()
                 .Contain("Inner exception")
                 .And.Contain("inner detail");
    }

    [Fact]
    public void CanHandle_should_accept_only_Win32_exceptions()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        formatter.CanHandle(new Win32Exception(1)).Should().BeTrue();
        formatter.CanHandle(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void IsWriter_should_be_false_because_the_formatter_only_produces_text()
    {
        new Win32ExceptionMessageFormatter().IsWriter.Should().BeFalse();
    }
}
