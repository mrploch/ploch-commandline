using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

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
