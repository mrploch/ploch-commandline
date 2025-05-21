using Ploch.CommandLine.Spectre.Commands;
using Ploch.Tools.SystemProfiles.UI.ConsoleUI.WeatherForecasts;
using Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestAsyncCommand(ICommandSettingsValidator<TestCommandSettings> validator, IExceptionHandler<AsyncAppCommand<TestCommandSettings>> exceptionHandler)
    : AsyncAppCommand<TestCommandSettings>(validator, exceptionHandler)
{
    protected override Task<ExitCode> DoExecuteAsync(CommandContext context, TestCommandSettings settings) => Task.FromResult(ExitCode.Success);
}
