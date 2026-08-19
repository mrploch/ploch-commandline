using FluentAssertions;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;
#pragma warning disable CA2201
public class ExceptionMessageFormatterTests
{
    [Fact]
    public void GetMessage_should_include_inner_exception_message_when_present()
    {
        var formatter = new BaseExceptionMessageFormatter<Exception>();

        var innerException = new InvalidOperationException("Inner exception message");
        var exception = new ArgumentException("Outer exception message", innerException);

        var result = formatter.GetMessage(exception);

        result.Should().ContainAll("<ArgumentException>", "Outer exception message", "<InvalidOperationException>", "Inner exception message");
    }

    [Fact]
    public void GetMessage_should_return_formatted_message_for_exception_without_inner_exception()
    {
        var formatter = new BaseExceptionMessageFormatter<Exception>();
        var exception = new Exception("Test exception message");

        var result = formatter.GetMessage(exception);

        result.Should().ContainAll("<Exception>", "Test exception message");
    }

    [Fact]
    public void GetMessage_should_throw_argument_null_exception_when_message_is_null()
    {
        var formatter = new BaseExceptionMessageFormatter<Exception>();

        Action act = () => formatter.GetMessage(null);

        act.Should().Throw<ArgumentNullException>();
    }
}
#pragma warning restore CA2201
