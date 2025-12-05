using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestCommand(ICommandSettingsValidator<TestCommandSettings> validator, IExceptionHandler<TestCommand> exceptionHandler)
    : AppCommand<TestCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext context, TestCommandSettings settings) => ExitCode.Success;
}
