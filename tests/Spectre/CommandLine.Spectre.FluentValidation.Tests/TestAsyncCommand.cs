using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestAsyncCommand(ICommandSettingsValidator<TestCommandSettings> validator, IExceptionHandler<TestAsyncCommand> exceptionHandler)
    : AsyncAppCommand<TestCommandSettings>(validator, exceptionHandler)
{
    protected override Task<ExitCode> DoExecuteAsync(CommandContext context, TestCommandSettings settings) => Task.FromResult(ExitCode.Success);
}
