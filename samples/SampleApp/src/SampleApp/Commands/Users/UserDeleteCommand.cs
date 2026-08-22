using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users;

/// <summary>
///     Command to delete a user by ID. Demonstrates injecting an <see cref="ILogger{TCategoryName}" /> alongside
///     <see cref="IOutput" />: the console output is what the user reads, the log is what an operator inspects later.
///     It also shows the usual contract for a destructive command — confirm interactively, or require an explicit
///     <c>--force</c> when there is nobody to ask.
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
            Output.MarkupLineInterpolated($"[dim]Verbose: confirmation prompt is {(settings.Force ? "suppressed" : "enabled")}.[/]");
        }

        if (!settings.Force)
        {
            var refusal = ConfirmDeletion(settings.Id);
            if (refusal is not null)
            {
                return refusal.Value;
            }
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

    /// <summary>
    ///     Asks the operator to confirm the deletion, or explains how to skip the prompt when the console
    ///     cannot answer (a pipeline, a redirected stream).
    /// </summary>
    /// <param name="userId">The identifier of the user about to be deleted.</param>
    /// <returns>
    ///     <see langword="null" /> when the deletion may proceed; otherwise the exit code to return.
    ///     A missing <c>--force</c> on a non-interactive console is an input problem
    ///     (<see cref="ExitCode.InvalidInput" />); answering "no" at the prompt is a deliberate
    ///     cancellation (<see cref="ExitCode.Cancelled" />).
    /// </returns>
    private ExitCode? ConfirmDeletion(int userId)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            Output.MarkupLineInterpolated($"[yellow]Refusing to delete user {userId} without confirmation. Re-run with --force.[/]");

            return ExitCode.InvalidInput;
        }

        return AnsiConsole.Confirm($"Delete user {userId}?", defaultValue: false) ? null : ExitCode.Cancelled;
    }
}
