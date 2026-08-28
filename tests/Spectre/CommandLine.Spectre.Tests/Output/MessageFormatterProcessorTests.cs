using System.Globalization;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for the formatter/writer dispatch. <see cref="MessageFormatterProcessor.WriteMessage{TMessage}" />
///     reports whether a writer handled the message; before that it returned void and
///     <c>AnsiConsoleMarkupOutput.Write</c> unconditionally wrote a fallback, printing handled messages twice.
/// </summary>
public class MessageFormatterProcessorTests
{
    [Fact]
    public void WriteMessage_should_report_the_writer_that_handled_the_message()
    {
        var writer = new RecordingStringWriter();
        var processor = new MessageFormatterProcessor([], [writer]);

        var handled = processor.WriteMessage("hello");

        handled.Should().NotBeNull();
        writer.Written.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public void WriteMessage_should_report_no_writer_when_none_can_handle_the_message()
    {
        var writer = new RecordingStringWriter();
        var processor = new MessageFormatterProcessor([], [writer]);

        var handled = processor.WriteMessage(42);

        handled.Should().BeNull();
        writer.Written.Should().BeEmpty("a writer that cannot handle the message must not be invoked");
    }

    [Fact]
    public void WriteMessage_should_hand_the_original_message_to_the_writer_rather_than_its_formatted_text()
    {
        var writer = new RecordingExceptionWriter();
        var processor = new MessageFormatterProcessor([new ExceptionMessageFormatter()], [writer]);
        var exception = new InvalidOperationException("probe failure");

        var handled = processor.WriteMessage(exception);

        handled.Should().NotBeNull();
        writer.Written.Should()
              .ContainSingle()
              .Which.Should()
              .BeSameAs(exception, "the writer is chosen by the type of the message, so it has to be given that same message");
    }

    [Fact]
    public void WriteMessage_should_supply_itself_as_the_formatter_processor_so_the_writer_can_format_the_message()
    {
        var writer = new RecordingExceptionWriter();
        var processor = new MessageFormatterProcessor([], [writer]);

        processor.WriteMessage(new InvalidOperationException("probe failure"));

        writer.Processors.Should().ContainSingle().Which.Should().BeSameAs(processor, "formatting is the writer's job and it needs the processor to do it");
    }

    [Fact]
    public void WriteMessage_should_report_no_writer_for_a_null_message()
    {
        var processor = new MessageFormatterProcessor([], [new RecordingStringWriter()]);

        processor.WriteMessage<string>(null).Should().BeNull();
    }

    [Fact]
    public void GetMessageText_should_return_empty_string_for_a_null_message()
    {
        var processor = new MessageFormatterProcessor([], []);

        processor.GetMessageText<string>(null).Should().BeEmpty();
    }

    [Fact]
    public void GetMessageText_should_wrap_the_result_in_the_supplied_markup_tag()
    {
        var processor = new MessageFormatterProcessor([], []);

        var result = processor.GetMessageText("value", "bold");

        result.Should().Be("[bold]value[/]");
    }

    [Fact]
    public void GetMessageText_should_use_a_registered_formatter_when_one_can_handle_the_message()
    {
        var processor = new MessageFormatterProcessor([new StringMessageFormatter()], []);

        var result = processor.GetMessageText("value");

        result.Should().Be("value");
    }

    [Fact]
    public void GetMessageText_should_fall_back_to_ToString_when_no_formatter_matches()
    {
        var processor = new MessageFormatterProcessor([], []);

        processor.GetMessageText(42).Should().Be("42");
    }

    [Fact]
    public void GetMessageText_should_apply_a_registered_formatter_before_the_markup_tag()
    {
        var processor = new MessageFormatterProcessor([new UpperCasingFormatter()], []);

        processor.GetMessageText("value", "underline").Should().Be("[underline]VALUE[/]", "the formatter runs first and the tag wraps its output");
    }

    [Fact]
    public void GetMessageText_should_return_an_empty_formattable_string_for_a_null_interpolated_message()
    {
        var processor = new MessageFormatterProcessor([], []);

        processor.GetMessageText((FormattableString?)null, formatProvider: CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture).Should().BeEmpty();
    }

    [Fact]
    public void GetMessageText_should_format_the_arguments_of_an_interpolated_message_through_the_formatters()
    {
        var processor = new MessageFormatterProcessor([new UpperCasingFormatter()], []);

        FormattableString message = $"greeting: {"hello"}";

        var result = processor.GetMessageText(message, formatProvider: CultureInfo.InvariantCulture);

        result.ToString(CultureInfo.InvariantCulture).Should().Be("greeting: HELLO", "each argument is passed through the matching formatter");
    }

    [Fact]
    public void GetMessageText_should_substitute_an_empty_string_for_a_null_interpolated_argument()
    {
        var processor = new MessageFormatterProcessor([new UpperCasingFormatter()], []);

        FormattableString message = $"value: [{(string?)null}]";

        var result = processor.GetMessageText(message, formatProvider: CultureInfo.InvariantCulture);

        result.ToString(CultureInfo.InvariantCulture).Should().Be("value: []");
    }

    [Fact]
    public void GetMessageText_should_fall_back_to_ToString_for_an_interpolated_argument_with_no_formatter()
    {
        var processor = new MessageFormatterProcessor([], []);

        FormattableString message = $"count: {17}";

        var result = processor.GetMessageText(message, formatProvider: CultureInfo.InvariantCulture);

        result.ToString(CultureInfo.InvariantCulture).Should().Be("count: 17");
    }

    [Fact]
    public void GetMessageText_should_wrap_an_interpolated_message_in_the_markup_tag()
    {
        var processor = new MessageFormatterProcessor([], []);

        FormattableString message = $"count: {17}";

        var result = processor.GetMessageText(message, "red", formatProvider: CultureInfo.InvariantCulture);

        result.ToString(CultureInfo.InvariantCulture).Should().Be("[red]count: 17[/]");
    }

    [Fact]
    public void GetMessageText_should_keep_the_interpolated_arguments_separate_from_the_format_string()
    {
        var processor = new MessageFormatterProcessor([], []);

        FormattableString message = $"{1}-{2}";

        var result = processor.GetMessageText(message, formatProvider: CultureInfo.InvariantCulture);

        result.Format.Should().Be("{0}-{1}", "the result stays a composite format string rather than being flattened early");
        result.GetArguments().Should().Equal(1, 2);
    }

    [Fact]
    public void GetMessageText_should_preserve_the_format_specifier_of_an_unformatted_argument()
    {
        var processor = new MessageFormatterProcessor([], []);

        FormattableString message = $"total: {1234.5:N2}";

        var result = processor.GetMessageText(message, formatProvider: CultureInfo.InvariantCulture);

        result.ToString(CultureInfo.InvariantCulture)
              .Should()
              .Be("total: 1,234.50", "stringifying the argument first would leave N2 applied to a string, which silently drops it");
    }

    [Fact]
    public void GetMessageText_should_keep_the_arguments_of_a_markup_tagged_interpolated_message()
    {
        var processor = new MessageFormatterProcessor([], []);

        FormattableString message = $"path: {"[archive]"}";

        var result = processor.GetMessageText(message, "red", formatProvider: CultureInfo.InvariantCulture);

        result.Format.Should().Be("[red]path: {0}[/]", "only the tag is added; the message keeps its holes");
        result.GetArguments().Should().Equal("[archive]");
    }

    [Fact]
    public void GetMessageText_should_escape_a_tagged_message_that_contains_markup_characters()
    {
        var processor = new MessageFormatterProcessor([], []);

        var result = processor.GetMessageText("Value [archive] is invalid", "red", formatProvider: CultureInfo.InvariantCulture);

        result.Should().Be("[red]Value [[archive]] is invalid[/]", "the caller asked for a colour, not for their data to be parsed as markup");
    }

    /// <summary>A formatter that visibly transforms its input, so a test can prove the formatter was actually applied.</summary>
    private sealed class UpperCasingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
            message?.ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>Records what a writer registered for <see cref="Exception" /> was actually handed.</summary>
    private sealed class RecordingExceptionWriter : TypeMessageWriter<Exception>
    {
        public List<Exception?> Written { get; } = [];

        public List<IMessageFormatterProcessor?> Processors { get; } = [];

        public override void Write(Exception? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
        {
            Written.Add(message);
            Processors.Add(formatterProcessor);
        }
    }

    private sealed class RecordingStringWriter : TypeMessageWriter<string>
    {
        public List<string?> Written { get; } = [];

        public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) => Written.Add(message);
    }
}
