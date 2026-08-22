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

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "set", null);

    private ConfigSetCommand CreateCommand() => new(_validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object);
}
