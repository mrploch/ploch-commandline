using System.Globalization;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

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
