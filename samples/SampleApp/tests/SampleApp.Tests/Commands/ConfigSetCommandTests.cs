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

    [Fact]
    public void Execute_should_set_config_and_return_success()
    {
        var settings = new ConfigSetCommandSettings
        {
            Key = "TestKey",
            Value = "TestValue",
            Scope = "user"
        };

        var command = new ConfigSetCommand(_validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object);
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "set", null);

        var result = command.Execute(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
    }
}
