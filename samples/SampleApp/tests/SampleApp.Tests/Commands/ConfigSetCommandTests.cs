using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Config;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class ConfigSetCommandTests
{
    private readonly Mock<ICommandSettingsValidator<ConfigSetCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();

    [Theory]
    [InlineData("user")]
    [InlineData("system")]
    [InlineData("SYSTEM")]
    public void Execute_should_return_success_for_a_supported_scope(string scope)
    {
        var settings = new ConfigSetCommandSettings { Key = "TestKey", Value = "TestValue", Scope = scope };

        var result = CreateCommand().Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
    }

    [Fact]
    public void Execute_should_return_invalid_input_for_an_unsupported_scope()
    {
        var settings = new ConfigSetCommandSettings { Key = "TestKey", Value = "TestValue", Scope = "machine" };

        var result = CreateCommand().Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
    }

    /// <summary>
    ///     The command echoes the value back, so a secret passed on the command line would otherwise land in
    ///     console output and any CI log capturing it - the same disclosure `config get` and `config show`
    ///     already guard against.
    /// </summary>
    [Fact]
    public void Execute_should_redact_a_value_whose_key_names_a_secret()
    {
        var written = new List<string>();
        var settings = new ConfigSetCommandSettings { Key = "SampleAppSettings:ApiKey", Value = "super-secret-value", Scope = "user" };

        var result = CreateCommand(written).Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        written.Should().NotContainMatch("*super-secret-value*", "a secret must not be echoed back to the console");
        written.Should().ContainMatch("*redacted*");
    }

    /// <summary>
    ///     The rejection path is the command's other output call, and it is asserted elsewhere only by exit code.
    ///     Nothing otherwise stops a later, more "helpful" error message from including the value the user typed -
    ///     which for a secret would leak it on the one path that never reaches the redaction above.
    /// </summary>
    [Fact]
    public void Execute_should_not_echo_the_value_when_it_rejects_the_scope()
    {
        var written = new List<string>();
        var settings = new ConfigSetCommandSettings { Key = "SampleAppSettings:ApiKey", Value = "super-secret-value", Scope = "machine" };

        var result = CreateCommand(written).Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
        written.Should().NotContainMatch("*super-secret-value*", "the rejection path must not disclose what the redaction path protects");
    }

    [Fact]
    public void Execute_should_echo_an_ordinary_value_unchanged()
    {
        var written = new List<string>();
        var settings = new ConfigSetCommandSettings { Key = "SampleAppSettings:Environment", Value = "Development", Scope = "user" };

        var result = CreateCommand(written).Execute(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        written.Should().ContainMatch("*Development*", "the redaction must be driven by the key, not applied to everything");
    }

    private ConfigSetCommand CreateCommand(List<string> written)
    {
        var outputMock = new Mock<IOutput>();
        outputMock.Setup(output => output.MarkupLineInterpolated(It.IsAny<FormattableString>()))
                  .Callback<FormattableString>(message => written.Add(message.ToString()))
                  .Returns(() => outputMock.Object);

        return new ConfigSetCommand(_validatorMock.Object, _exceptionHandlerMock.Object, outputMock.Object);
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "set", null);

    private ConfigSetCommand CreateCommand() => new(_validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object);
}
