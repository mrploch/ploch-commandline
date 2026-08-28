namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for <see cref="ConsoleAppInfoExtensions.Validate" />, which guards the application banner.
/// </summary>
public class ConsoleAppInfoExtensionsTests
{
    [Fact]
    public void Validate_should_not_throw_when_the_name_is_present()
    {
        var appInfo = new ConsoleAppInfo { Name = "My Application" };

        Action act = appInfo.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_should_throw_when_the_name_is_empty_or_whitespace(string name)
    {
        var appInfo = new ConsoleAppInfo { Name = name };

        Action act = appInfo.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*null, empty, or whitespace*");
    }

    [Fact]
    public void Validate_message_should_describe_both_rejected_states()
    {
        var appInfo = new ConsoleAppInfo { Name = string.Empty };

        Action act = appInfo.Validate;

        act.Should()
           .Throw<InvalidOperationException>()
           .Which.Message.Should()
           .Contain("empty", "the check rejects empty names as well as null ones, and the message must say so");
    }
}
