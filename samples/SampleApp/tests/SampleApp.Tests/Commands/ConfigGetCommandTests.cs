using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Config;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

/// <summary>
///     Cover for the disclosure policy on <see cref="ConfigGetCommand" />. The host adds an environment-variable
///     configuration provider, so an unguarded lookup by user-supplied key reads any environment variable of the
///     process - which is what this command used to do.
/// </summary>
public class ConfigGetCommandTests
{
    private readonly Mock<ICommandSettingsValidator<ConfigGetCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly List<string> _written = [];

    [Theory]
    [InlineData("AWS_SECRET_ACCESS_KEY")]
    [InlineData("PATH")]
    [InlineData("LoggingSecrets:ApiKey")]
    public void Execute_should_refuse_a_key_outside_the_applications_own_sections(string key)
    {
        var command = CreateCommand(new Dictionary<string, string?> { [key] = "super-secret-value" });

        var result = command.Execute(CreateContext(), new ConfigGetCommandSettings { Key = key }, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
        _written.Should().NotContainMatch("*super-secret-value*", "a value outside the allow-list must never reach the console");
    }

    [Fact]
    public void Execute_should_redact_a_value_whose_key_names_a_secret()
    {
        var command = CreateCommand(new Dictionary<string, string?> { ["Serilog:WriteTo:0:Args:apiKey"] = "super-secret-value" });
        var settings = new ConfigGetCommandSettings { Key = "Serilog:WriteTo:0:Args:apiKey" };

        var result = command.Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        _written.Should().NotContainMatch("*super-secret-value*", "an allowed section does not make the leaves inside it safe");
        _written.Should().ContainMatch("*redacted*");
    }

    [Fact]
    public void Execute_should_render_an_ordinary_value_from_an_allowed_section()
    {
        var command = CreateCommand(new Dictionary<string, string?> { ["SampleAppSettings:Environment"] = "Development" });
        var settings = new ConfigGetCommandSettings { Key = "SampleAppSettings:Environment" };

        var result = command.Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        _written.Should().ContainMatch("*Development*", "the command still has to do its job");
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "get", null);

    private ConfigGetCommand CreateCommand(Dictionary<string, string?> values)
    {
        var outputMock = new Mock<IOutput>();
        outputMock.Setup(output => output.MarkupLineInterpolated(It.IsAny<FormattableString>()))
                  .Callback<FormattableString>(message => _written.Add(message.ToString()))
                  .Returns(() => outputMock.Object);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new ConfigGetCommand(_validatorMock.Object, _exceptionHandlerMock.Object, outputMock.Object, configuration);
    }
}
