using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users;

/// <summary>
///     Asynchronous command demonstrating <see cref="AsyncAppCommand{TSettings}" /> with dependency injection.
/// </summary>
public class UserAddCommand(CommandArgumentsRootProcessor settingsProcessor,
                            ICommandSettingsValidator<UserAddCommandSettings> validator,
                            IExceptionHandler exceptionHandler,
                            IOutput output,
                            IUserService userService) : AsyncAppCommand<UserAddCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    /// <inheritdoc />
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, UserAddCommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Verbose)
        {
            Output.MarkupLineInterpolated($"[dim]Verbose: email='{settings.Email}', role='{settings.Role}'.[/]");
        }

        Output.MarkupLineInterpolated($"[cyan]Creating new user account for[/] [bold yellow]{settings.Name}[/]...");

        var user = await userService.CreateUserAsync(settings.Name, settings.Email, settings.Role, cancellationToken);

        // Everything the user typed is escaped before it reaches Markup: a name such as "A[dmin]"
        // would otherwise be parsed as markup and throw after the account had already been created.
        // MarkupLineInterpolated does this for its holes automatically; a Markup built from a string
        // does not.
        var panel = new Panel(new Markup($"[bold]User ID:[/] {user.Id}\n" +
                                         $"[bold]Name:[/] {Markup.Escape(user.Name)}\n" +
                                         $"[bold]Email:[/] {Markup.Escape(user.Email)}\n" +
                                         $"[bold]Role:[/] [green]{Markup.Escape(user.Role)}[/]\n" +
                                         $"[bold]Active:[/] {(user.IsActive ? "[green]Yes[/]" : "[red]No[/]")}\n" +
                                         $"[bold]Created:[/] {user.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC"))
        {
            Header = new PanelHeader("[bold green]User Created Successfully[/]"),
            Border = BoxBorder.Rounded
        };

        Output.Write(panel);

        return ExitCode.Success;
    }
}
