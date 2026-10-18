using System.ComponentModel;
using System.Globalization;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for <see cref="Win32ExceptionMessageFormatter" />, which adds the native error code to the rendered text.
/// </summary>
public class Win32ExceptionMessageFormatterTests
{
    [Fact]
    public void GetMessage_should_include_the_native_error_code()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        var result = formatter.GetMessage(new Win32Exception(5), formatProvider: CultureInfo.InvariantCulture);

        result.Should().Contain("<Error Code: 5>").And.Contain(nameof(Win32Exception));
    }

    [Fact]
    public void GetMessage_should_append_the_inner_exception_details_only_when_there_is_one()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        formatter.GetMessage(new Win32Exception(2, "no inner"), formatProvider: CultureInfo.InvariantCulture).Should().NotContain("Inner exception");
        formatter.GetMessage(new Win32Exception("outer", new InvalidOperationException("inner detail")), formatProvider: CultureInfo.InvariantCulture)
                 .Should()
                 .Contain("Inner exception")
                 .And.Contain("inner detail");
    }

    [Fact]
    public void CanHandle_should_accept_only_Win32_exceptions()
    {
        var formatter = new Win32ExceptionMessageFormatter();

        formatter.CanHandle(new Win32Exception(1)).Should().BeTrue();
        formatter.CanHandle(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void IsWriter_should_be_false_because_the_formatter_only_produces_text()
    {
        new Win32ExceptionMessageFormatter().IsWriter.Should().BeFalse();
    }
}
