using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for the console output adapter. The dispatch inside <see cref="AnsiConsoleMarkupOutput.Write{TMessage}" />
///     is the path that previously wrote writer-handled messages twice, and the bold/error helpers are the only
///     place the markup tags reach the formatter processor.
/// </summary>
public class AnsiConsoleMarkupOutputTests
{
    [Fact]
    public void Write_should_render_a_plain_string_as_markup()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.Write("[bold]emphasised[/]");

        console.Output.Should().Be("emphasised", "the markup tags are interpreted, not printed literally");
    }

    [Fact]
    public void Write_should_render_an_interpolated_string_with_its_arguments_escaped()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.Write<FormattableString>($"value is {"[not-markup]"}");

        console.Output.Should().Be("value is [not-markup]", "interpolated arguments are escaped rather than parsed as markup");
    }

    [Fact]
    public void Write_should_use_the_supplied_format_provider_for_an_interpolated_string()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.Write<FormattableString>($"{1234.5:N2}", CultureInfo.GetCultureInfo("de-DE"));

        console.Output.Should().Be("1.234,50", "the German culture groups with '.' and separates decimals with ','");
    }

    [Fact]
    public void Write_should_render_a_renderable_through_the_console()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.Write<IRenderable>(new Text("renderable text"));

        console.Output.Should().Contain("renderable text");
    }

    [Fact]
    public void Write_should_render_a_renderable_passed_to_the_dedicated_overload()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        IRenderable renderable = new Text("dedicated overload");

        var chained = output.Write(renderable);

        console.Output.Should().Contain("dedicated overload");
        chained.Should().BeSameAs(output);
    }

    [Fact]
    public void Write_should_delegate_to_a_registered_writer_exactly_once()
    {
        using var console = new RecordingConsole();
        var writer = new CountingEnumerableWriter();
        var output = new AnsiConsoleMarkupOutput(console.Console, new MessageFormatterProcessor([], [writer]));

        int[] items = [1, 2, 3];

        output.Write(items);

        writer.WriteCount.Should().Be(1);
        console.Output.Should().BeEmpty("a message a writer handled must not also be written by the fallback path");
    }

    [Fact]
    public void Write_should_fall_back_to_ToString_when_no_writer_handles_the_message()
    {
        using var console = new RecordingConsole();
        var output = new AnsiConsoleMarkupOutput(console.Console, new MessageFormatterProcessor([], []));

        output.Write(new UnhandledMessage());

        console.Output.Should().Be(UnhandledMessage.Text);
    }

    [Fact]
    public void Write_should_render_nothing_for_a_null_message()
    {
        using var console = new RecordingConsole();
        var output = new AnsiConsoleMarkupOutput(console.Console, new MessageFormatterProcessor([], []));

        output.Write<UnhandledMessage?>(null);

        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void WriteLine_should_terminate_the_line_after_a_string()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine("line");

        console.Output.Should().Be("line" + Environment.NewLine);
    }

    [Fact]
    public void WriteLine_should_terminate_the_line_after_an_interpolated_string()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine<FormattableString>($"count: {7}");

        console.Output.Should().Be("count: 7" + Environment.NewLine);
    }

    [Fact]
    public void WriteLine_should_render_a_non_string_message_via_ToString()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine(new UnhandledMessage());

        console.Output.Should().Be(UnhandledMessage.Text + Environment.NewLine);
    }

    [Fact]
    public void WriteLine_should_write_only_a_line_terminator_for_a_null_message()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine<UnhandledMessage?>(null);

        console.Output.Should().Be(Environment.NewLine);
    }

    [Fact]
    public void WriteLine_should_write_only_a_line_terminator_when_called_without_a_message()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine();

        console.Output.Should().Be(Environment.NewLine);
    }

    [Fact]
    public void EndLine_should_write_a_line_terminator()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.EndLine();

        console.Output.Should().Be(Environment.NewLine);
    }

    [Fact]
    public void MarkupInterpolated_should_escape_the_interpolated_arguments()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.MarkupInterpolated($"[bold]{"[literal]"}[/]");

        console.Output.Should().Be("[literal]");
    }

    [Fact]
    public void MarkupLineInterpolated_should_escape_the_arguments_and_terminate_the_line()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.MarkupLineInterpolated($"[bold]{"[literal]"}[/]");

        console.Output.Should().Be("[literal]" + Environment.NewLine);
    }

    [Fact]
    public void WriteBold_and_WriteBoldLine_should_ask_the_processor_for_the_bold_markup_tag()
    {
        using var console = new RecordingConsole();
        var processor = new RecordingFormatterProcessor();
        var output = new AnsiConsoleMarkupOutput(console.Console, processor);

        output.WriteBold("message");
        output.WriteBoldLine("message");

        processor.RequestedTags.Should().Equal("bold", "bold");
    }

    [Fact]
    public void WriteError_and_WriteErrorLine_should_ask_the_processor_for_the_red_markup_tag()
    {
        using var console = new RecordingConsole();
        var processor = new RecordingFormatterProcessor();
        var output = new AnsiConsoleMarkupOutput(console.Console, processor);

        output.WriteError("message");
        output.WriteErrorLine("message");

        processor.RequestedTags.Should().Equal("red", "red");
    }

    [Fact]
    public void WriteErrorLine_should_render_the_message_and_terminate_the_line()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteErrorLine("it failed");

        console.Output.Should().Be("it failed" + Environment.NewLine, "the red markup is consumed by the renderer, so only the text survives");
    }

    [Fact]
    public void WriteBold_should_render_the_message_without_a_line_terminator()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteBold("strong");

        console.Output.Should().Be("strong");
    }

    [Fact]
    public void WriteException_should_render_the_exception_message()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteException(new InvalidOperationException("the failure detail"));

        console.Output.Should().Contain("the failure detail");
    }

    [Fact]
    public void WriteException_should_write_nothing_for_a_null_exception()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteException<Exception>(null);

        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void Write_should_return_the_same_instance_so_calls_can_be_chained()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        var chained = output.Write("a").WriteLine("b").WriteBold("c").WriteError("d").EndLine();

        chained.Should().BeSameAs(output);
        console.Output.Should().Be("ab" + Environment.NewLine + "cd" + Environment.NewLine);
    }

    [Fact]
    public void WriteError_should_print_bracketed_data_literally_instead_of_parsing_it_as_markup()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        var write = () => output.WriteError("Value [archive] is invalid");

        write.Should().NotThrow("the caller asked for red text, not for their message to be read as a style tag");
        console.Output.Should().Be("Value [archive] is invalid");
    }

    [Fact]
    public void WriteBold_should_print_a_bracketed_path_literally()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteBold(@"C:\logs\[archive]\x.txt");

        console.Output.Should().Be(@"C:\logs\[archive]\x.txt");
    }

    [Fact]
    public void WriteErrorLine_should_print_bracketed_data_literally_and_terminate_the_line()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteErrorLine("failed on [item]");

        console.Output.Should().Be("failed on [item]" + Environment.NewLine);
    }

    [Fact]
    public void WriteLine_should_dispatch_through_the_same_pipeline_as_Write()
    {
        using var console = new RecordingConsole();
        var output = CreateOutput(console);

        output.WriteLine(new UnhandledMessage());

        console.Output.Should().Be(UnhandledMessage.Text + Environment.NewLine,
                                   "WriteLine and Write must agree on how a message is rendered");
    }

    private static AnsiConsoleMarkupOutput CreateOutput(RecordingConsole console) =>
        new(console.Console, new MessageFormatterProcessor([new StringMessageFormatter()], []));

    private sealed class UnhandledMessage
    {
        public const string Text = "unhandled-message";

        public override string ToString() => Text;
    }

    /// <summary>Counts how many times the writer was invoked, which is what the double-write regression turns on.</summary>
    private sealed class CountingEnumerableWriter : TypeMessageWriter<System.Collections.IEnumerable>
    {
        public int WriteCount { get; private set; }

        public override void Write(System.Collections.IEnumerable? message, IMessageFormatterProcessor? formatterProcessor = null) => WriteCount++;
    }

    /// <summary>Records the markup tags the output adapter asks for, which is otherwise invisible once rendered.</summary>
    private sealed class RecordingFormatterProcessor : IMessageFormatterProcessor
    {
        public List<string?> RequestedTags { get; } = [];

        public FormattableString GetMessageText(FormattableString? message, string? markupTag = null)
        {
            RequestedTags.Add(markupTag);

            return message ?? $"";
        }

        public string? GetMessageText<TMessage>(TMessage? message, string? markupTag = null)
        {
            RequestedTags.Add(markupTag);

            return message?.ToString();
        }

        public bool WriteMessage<TMessage>(TMessage message) => false;
    }
}
