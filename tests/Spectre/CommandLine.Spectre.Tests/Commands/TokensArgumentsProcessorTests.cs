using System.Globalization;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for token substitution into command settings properties marked with
///     <see cref="SupportsTokensAttribute" />. Tokens resolve in UTC.
/// </summary>
public class TokensArgumentsProcessorTests
{
    [Fact]
    public void ProcessArguments_should_substitute_the_date_token()
    {
        var settings = new TokenSettings { Path = "logs/{date}/app.log" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Path.Should().Be($"logs/{DateTime.UtcNow:yyyy-MM-dd}/app.log");
    }

    [Fact]
    public void ProcessArguments_should_substitute_the_datetime_token()
    {
        var settings = new TokenSettings { Path = "{datetime}" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Path.Should().NotBe("{datetime}").And.StartWith(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ProcessArguments_should_match_tokens_case_insensitively()
    {
        var settings = new TokenSettings { Path = "{DATE}" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Path.Should().NotContain("{DATE}");
    }

    [Fact]
    public void ProcessArguments_should_produce_a_path_safe_value_when_the_attribute_requests_it()
    {
        var settings = new TokenSettings { Path = "{datetime}" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Path.Should().NotContain(":", "a path-safe token must not contain characters illegal in a file name");
    }

    [Fact]
    public void ProcessArguments_should_leave_properties_without_the_attribute_untouched()
    {
        var settings = new TokenSettings { Path = "{date}", Untagged = "{date}" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Untagged.Should().Be("{date}");
    }

    [Fact]
    public void ProcessArguments_should_leave_a_value_without_tokens_unchanged()
    {
        var settings = new TokenSettings { Path = "no tokens here" };

        new TokensArgumentsProcessor().ProcessArguments(settings);

        settings.Path.Should().Be("no tokens here");
    }

    [Fact]
    public void ProcessArguments_should_tolerate_a_null_property_value()
    {
        var settings = new TokenSettings { Path = null };

        var act = () => new TokensArgumentsProcessor().ProcessArguments(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void ProcessArguments_should_be_safe_to_call_twice_on_the_same_instance()
    {
        var processor = new TokensArgumentsProcessor();

        processor.ProcessArguments(new TokenSettings { Path = "{date}" });
        var act = () => processor.ProcessArguments(new TokenSettings { Path = "{date}" });

        act.Should().NotThrow("collected properties must be reset between invocations");
    }

    private sealed class TokenSettings : CommandSettings
    {
        [SupportsTokens]
        public string? Path { get; set; }

        public string? Untagged { get; set; }
    }
}
