using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestCommand(CommandArgumentsRootProcessor settingsProcessor,
                         ICommandSettingsValidator<TestCommandSettings> validator,
                         IExceptionHandler exceptionHandler,
                         IOutput output)
    : AppCommand<TestCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override ExitCode DoExecute(CommandContext context, TestCommandSettings settings, CancellationToken cancellationToken) => ExitCode.Success;
}
