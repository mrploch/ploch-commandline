using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users;

/// <summary>
///     Command to list users in the system formatted as a Spectre Table.
/// </summary>
public class UserListCommand(CommandArgumentsRootProcessor settingsProcessor,
                             ICommandSettingsValidator<UserListCommandSettings> validator,
                             IExceptionHandler exceptionHandler,
                             IOutput output,
                             IUserService userService) : AsyncAppCommand<UserListCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    private static readonly string[] SupportedFormats = ["table", "compact"];

    /// <inheritdoc />
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, UserListCommandSettings settings, CancellationToken cancellationToken)
    {
        // Settings that Spectre cannot express as a type (here: an enumeration of two literal
        // formats) are checked by the command itself and reported with ExitCode.InvalidInput.
        if (!SupportedFormats.Contains(settings.Format, StringComparer.OrdinalIgnoreCase))
        {
            Output.MarkupLineInterpolated($"[red]Unsupported format '{settings.Format}'. Supported formats: {string.Join(", ", SupportedFormats)}.[/]");

            return ExitCode.InvalidInput;
        }

        if (settings.Verbose)
        {
            Output.MarkupLineInterpolated($"[dim]Verbose: format='{settings.Format}', active only='{settings.ActiveOnly}'.[/]");
        }

        Output.MarkupLineInterpolated($"[dim]Retrieving users (active only: {settings.ActiveOnly})...[/]");

        var users = (await userService.GetUsersAsync(settings.ActiveOnly, cancellationToken)).ToList();

        if (users.Count == 0)
        {
            Output.MarkupLineInterpolated($"[yellow]No users found matching the criteria.[/]");

            return ExitCode.Success;
        }

        if (string.Equals(settings.Format, "compact", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var user in users)
            {
                Output.MarkupLineInterpolated($"[[{user.Id}]] [bold]{user.Name}[/] <{user.Email}> ({user.Role}) - Active: {user.IsActive}");
            }
        }
        else
        {
            var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]Registered Users[/]");
            table.AddColumn("[yellow]ID[/]");
            table.AddColumn("[yellow]Name[/]");
            table.AddColumn("[yellow]Email[/]");
            table.AddColumn("[yellow]Role[/]");
            table.AddColumn("[yellow]Status[/]");
            table.AddColumn("[yellow]Created At[/]");

            foreach (var user in users)
            {
                var status = user.IsActive ? "[green]Active[/]" : "[red]Inactive[/]";
                table.AddRow(user.Id.ToString(), user.Name, user.Email, user.Role, status, user.CreatedAt.ToString("yyyy-MM-dd"));
            }

            AnsiConsole.Write(table);
        }

        Output.WriteLine();
        Output.MarkupLineInterpolated($"[green]Total users: {users.Count}[/]");

        return ExitCode.Success;
    }
}
