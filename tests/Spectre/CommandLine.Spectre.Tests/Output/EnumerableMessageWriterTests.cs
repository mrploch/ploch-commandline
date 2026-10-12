using System.Collections;
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

    /// <summary>
    ///     A null item inside the collection used to throw, because the no-processor branch called
    ///     <c>item.ToString()!</c> on it. Routing that branch through the shared fallback renders it as empty
    ///     instead, matching what EnumerableMessageFormatter has always done for the same input.
    /// </summary>
    [Fact]
    public void Write_should_render_a_null_item_as_empty_rather_than_throwing()
    {
        using var console = new RecordingConsole();
        var items = new string?[] { "first", null, "third" };

        var act = () => new EnumerableMessageWriter(console.Console).Write(items, formatProvider: CultureInfo.InvariantCulture);

        act.Should().NotThrow("one unusable item must not take down the whole listing");
        console.Output.Should().Contain("first").And.Contain("third");
    }

    private sealed class BracketingFormatter : TypeMessageFormatter<string>
    {
        public override string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) => $"<{message}>";
    }
}
