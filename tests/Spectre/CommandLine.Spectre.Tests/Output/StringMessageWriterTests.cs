using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="StringMessageWriter" />, the writer <c>IOutput.Write</c> selects for a string. Its
///     behaviour is the verbatim write and the null substitution; it must also leave the line terminator to
///     <c>IOutput.WriteLine</c>.
/// </summary>
public class StringMessageWriterTests
{
    [Fact]
    public void WritesLineTerminator_should_be_false_so_WriteLine_adds_the_terminator()
    {
        using var console = new RecordingConsole();

        IMessageWriter writer = new StringMessageWriter(console.Console);

        writer.WritesLineTerminator.Should().BeFalse("the writer renders inline, so WriteLine must still append its line break");
    }

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
