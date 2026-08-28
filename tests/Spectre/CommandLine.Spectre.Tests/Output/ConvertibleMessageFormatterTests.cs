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

        var result = formatter.GetMessage(message, formatProvider: CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetMessage_should_return_empty_string_when_message_is_null()
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(null, formatProvider: CultureInfo.InvariantCulture);

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Pins the fallback, so the provider is passed as null on purpose: supplying one here would assert the
    ///     provider path while claiming to cover the default, and would pass only on a machine whose current culture
    ///     happens to agree with it.
    /// </summary>
    [Fact]
    public void GetMessage_should_use_the_current_culture_when_no_provider_is_supplied()
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(1.5, formatProvider: null);

        result.Should().Be(1.5.ToString(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void GetMessage_should_prefer_the_supplied_provider_over_the_current_culture()
    {
        var formatter = new ConvertibleMessageFormatter();

        var result = formatter.GetMessage(1234.5, formatProvider: CultureInfo.GetCultureInfo("de-DE"));

        result.Should().Be("1234,5", "the provider the caller supplied must win");
    }

    [Fact]
    public void CanHandle_should_return_true_for_convertible_values()
    {
        var formatter = new ConvertibleMessageFormatter();

        formatter.CanHandle(42).Should().BeTrue();
        formatter.CanHandle(DateTime.UtcNow).Should().BeTrue();
    }
}
