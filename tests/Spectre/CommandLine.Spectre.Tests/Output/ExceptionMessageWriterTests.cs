using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Output;

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
