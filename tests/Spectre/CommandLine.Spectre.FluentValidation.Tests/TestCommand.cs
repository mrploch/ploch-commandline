using Ploch.CommandLine.Spectre.Commands;
using Ploch.Tools.SystemProfiles.UI.ConsoleUI.WeatherForecasts;
using Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestCommand(ICommandSettingsValidator<TestCommandSettings> validator, IExceptionHandler<AppCommand<TestCommandSettings>> exceptionHandler)
    : AppCommand<TestCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext context, TestCommandSettings settings) => ExitCode.Success;
}
