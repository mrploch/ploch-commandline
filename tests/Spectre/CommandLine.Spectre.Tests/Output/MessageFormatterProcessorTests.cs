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
    public void WriteMessage_should_report_true_when_a_registered_writer_handles_the_message()
    {
        var writer = new RecordingStringWriter();
        var processor = new MessageFormatterProcessor([], [writer]);

        var handled = processor.WriteMessage("hello");

        handled.Should().BeTrue();
        writer.Written.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public void WriteMessage_should_report_false_when_no_writer_can_handle_the_message()
    {
        var writer = new RecordingStringWriter();
        var processor = new MessageFormatterProcessor([], [writer]);

        var handled = processor.WriteMessage(42);

        handled.Should().BeFalse();
        writer.Written.Should().BeEmpty("a writer that cannot handle the message must not be invoked");
    }

    [Fact]
    public void WriteMessage_should_report_false_for_a_null_message()
    {
        var processor = new MessageFormatterProcessor([], [new RecordingStringWriter()]);

        processor.WriteMessage<string>(null).Should().BeFalse();
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

    private sealed class RecordingStringWriter : TypeMessageWriter<string>
    {
        public List<string?> Written { get; } = [];

        public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null) => Written.Add(message);
    }
}
