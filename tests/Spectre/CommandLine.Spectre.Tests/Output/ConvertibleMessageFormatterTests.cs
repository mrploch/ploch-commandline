using System.Globalization;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Regression cover for the formatter that is registered in DI for every <see cref="IConvertible" /> value.
///     It previously threw <see cref="NotImplementedException" />, so any attempt to write an int, bool or
///     DateTime through <c>IOutput</c> crashed the application.
/// </summary>
public class ConvertibleMessageFormatterTests
{
    [Theory]
    [InlineData(42, "42")]
    [InlineData(true, "True")]
    [InlineData(1.5, "1.5")]
    [InlineData("already a string", "already a string")]
    public void GetMessage_should_format_convertible_values_without_throwing(IConvertible message, string expected)
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(message);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetMessage_should_return_empty_string_when_message_is_null()
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetMessage_should_use_the_current_culture()
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(1.5);

        result.Should().Be(1.5.ToString(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void CanHandle_should_return_true_for_convertible_values()
    {
        var formatter = new ConvertibleMessageFormatter();

        formatter.CanHandle(42).Should().BeTrue();
        formatter.CanHandle(DateTime.UtcNow).Should().BeTrue();
    }
}
