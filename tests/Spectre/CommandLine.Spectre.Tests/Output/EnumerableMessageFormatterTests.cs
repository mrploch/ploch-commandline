using System.Collections;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for the formatter that renders a collection as a bulleted block.
/// </summary>
public class EnumerableMessageFormatterTests
{
    [Fact]
    public void GetMessage_should_return_an_empty_string_for_a_null_collection()
    {
        new EnumerableMessageFormatter().GetMessage(null).Should().BeEmpty();
    }

    [Fact]
    public void GetMessage_should_return_an_empty_string_for_an_empty_collection()
    {
        new EnumerableMessageFormatter().GetMessage(Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public void GetMessage_should_render_one_bulleted_line_per_item()
    {
        var formatter = new EnumerableMessageFormatter();
        string[] items = ["first", "second"];

        var result = formatter.GetMessage(items, new MessageFormatterProcessor([], []));

        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Be($"{Emoji.Known.BackhandIndexPointingRight} first");
        lines[1].Should().Be($"{Emoji.Known.BackhandIndexPointingRight} second");
    }

    [Fact]
    public void GetMessage_should_route_each_item_through_the_supplied_formatter_processor()
    {
        var formatter = new EnumerableMessageFormatter();
        var processor = new MessageFormatterProcessor([new BracketingFormatter()], []);
        string[] items = ["item"];

        var result = formatter.GetMessage(items, processor);

        result.Should().Contain("<item>", "the processor formats every item rather than the collection being flattened with ToString");
    }

    [Fact]
    public void GetMessage_should_render_empty_item_text_when_no_formatter_processor_is_supplied()
    {
        string[] items = ["item"];

        var result = new EnumerableMessageFormatter().GetMessage(items);

        result.Trim().Should().Be(Emoji.Known.BackhandIndexPointingRight, "without a processor there is nothing to turn the item into text");
    }

    [Fact]
    public void CanHandle_should_accept_collections_and_reject_scalars()
    {
        var formatter = new EnumerableMessageFormatter();

        formatter.CanHandle(new List<int>()).Should().BeTrue();
        formatter.CanHandle(42).Should().BeFalse();
        formatter.MessageType.Should().Be<IEnumerable>();
    }

    private sealed class BracketingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null) => $"<{message}>";
    }
}
