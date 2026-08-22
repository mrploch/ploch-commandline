using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users;

/// <summary>
///     Command to delete a user by ID. Demonstrates injecting an <see cref="ILogger{TCategoryName}" /> alongside
///     <see cref="IOutput" />: the console output is what the user reads, the log is what an operator inspects later.
/// </summary>
public class UserDeleteCommand(CommandArgumentsRootProcessor settingsProcessor,
                               ICommandSettingsValidator<UserDeleteCommandSettings> validator,
                               IExceptionHandler exceptionHandler,
                               IOutput output,
                               IUserService userService,
                               ILogger<UserDeleteCommand> logger) : AsyncAppCommand<UserDeleteCommandSettings>(settingsProcessor,
                                                                                                               validator,
                                                                                                               exceptionHandler,
                                                                                                               output)
{
    /// <inheritdoc />
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, UserDeleteCommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Verbose)
        {
            Output.MarkupLineInterpolated($"[dim]Verbose: deleting without confirmation prompt is {(settings.Force ? "enabled" : "disabled")}.[/]");
        }

        Output.MarkupLineInterpolated($"[dim]Deleting user with ID: {settings.Id} (force: {settings.Force})...[/]");

        var deleted = await userService.DeleteUserAsync(settings.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("[UserDeleteCommand] Delete requested for unknown user {UserId}", settings.Id);
            Output.MarkupLineInterpolated($"[red]User with ID {settings.Id} was not found.[/]");

            return ExitCode.Error;
        }

        logger.LogInformation("[UserDeleteCommand] Deleted user {UserId}", settings.Id);
        Output.MarkupLineInterpolated($"[green]User {settings.Id} deleted successfully.[/]");

        return ExitCode.Success;
    }
}
