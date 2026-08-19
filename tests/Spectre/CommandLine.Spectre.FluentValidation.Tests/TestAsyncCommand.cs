using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestAsyncCommand(CommandArgumentsRootProcessor settingsProcessor,
                              ICommandSettingsValidator<TestCommandSettings> validator,
                              IExceptionHandler exceptionHandler,
                              IOutput output)
    : AsyncAppCommand<TestCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override Task<ExitCode> DoExecuteAsync(CommandContext context, TestCommandSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(ExitCode.Success);
}
