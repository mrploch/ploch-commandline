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

    /// <summary>
    ///     The processor is an optional parameter, so omitting it must still render the items. This previously
    ///     produced a bare emoji per line because the null-conditional call returned null and the item's own text
    ///     was discarded.
    /// </summary>
    [Fact]
    public void GetMessage_should_render_the_item_text_when_no_formatter_processor_is_supplied()
    {
        string[] items = ["item"];

        var result = new EnumerableMessageFormatter().GetMessage(items);

        result.Trim().Should().Be($"{Emoji.Known.BackhandIndexPointingRight} item", "an optional processor must not mean the item is dropped");
    }

    /// <summary>
    ///     <c>GetMessageText</c> is declared <c>string?</c>, so a formatter may return null to suppress an item.
    ///     That is an answer, not an abstention: falling back to the item's own text would print exactly what the
    ///     formatter withheld. The probe item's ToString returns a distinctive value so an accidental fallback shows.
    /// </summary>
    [Fact]
    public void GetMessage_should_not_fall_back_to_ToString_when_the_processor_returns_null()
    {
        var formatter = new EnumerableMessageFormatter();
        object[] items = [new LoudItem()];

        var result = formatter.GetMessage(items, new MessageFormatterProcessor([new SuppressingFormatter()], []));

        result.Should().NotContain(LoudItem.Text, "a formatter that returns null is suppressing the item, not declining to format it");
        result.Trim().Should().Be(Emoji.Known.BackhandIndexPointingRight);
    }

    [Fact]
    public void GetMessage_should_preserve_an_empty_string_returned_by_the_processor()
    {
        var formatter = new EnumerableMessageFormatter();
        object[] items = [new LoudItem()];

        var result = formatter.GetMessage(items, new MessageFormatterProcessor([new EmptyingFormatter()], []));

        result.Should().NotContain(LoudItem.Text);
        result.Trim().Should().Be(Emoji.Known.BackhandIndexPointingRight);
    }

    [Fact]
    public void GetMessage_should_render_an_empty_entry_for_a_null_item_without_a_processor()
    {
        string?[] items = [null];

        var result = new EnumerableMessageFormatter().GetMessage(items);

        // Not Trim()ed: that would hide the separating space, so the assertion would pass even if the
        // emoji and the (empty) item text were concatenated without it.
        result.Should().Be($"{Emoji.Known.BackhandIndexPointingRight} {Environment.NewLine}", "a null item has no text of its own");
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

    /// <summary>An item whose ToString is unmistakable, so an unwanted fallback to it is visible in an assertion.</summary>
    private sealed class LoudItem
    {
        public const string Text = "RAW-ITEM-TEXT";

        public override string ToString() => Text;
    }

    /// <summary>A formatter that suppresses the item by returning null.</summary>
    private sealed class SuppressingFormatter : TypeMessageFormatter<LoudItem>
    {
        public override string GetMessage(LoudItem? message, IMessageFormatterProcessor? formatterProcessor = null) => null!;
    }

    /// <summary>A formatter that renders the item as an empty string.</summary>
    private sealed class EmptyingFormatter : TypeMessageFormatter<LoudItem>
    {
        public override string GetMessage(LoudItem? message, IMessageFormatterProcessor? formatterProcessor = null) => string.Empty;
    }
}
