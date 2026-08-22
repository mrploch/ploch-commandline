using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Files;

/// <summary>
///     Command demonstrating report generation for files.
/// </summary>
public class FileReportCommand(CommandArgumentsRootProcessor settingsProcessor,
                               ICommandSettingsValidator<FileReportCommandSettings> validator,
                               IExceptionHandler exceptionHandler,
                               IOutput output) : AsyncAppCommand<FileReportCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override Task<ExitCode> DoExecuteAsync(CommandContext context, FileReportCommandSettings settings, CancellationToken cancellationToken)
    {
        var panel = new Panel(new Markup($"[bold]Report Title:[/] {settings.Title}\n" +
                                         $"[bold]Source File:[/] {settings.Path}\n" +
                                         $"[bold]Status:[/] [green]Analyzed[/]\n" +
                                         $"[bold]Generated:[/] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"))
        {
            Header = new PanelHeader($"[bold cyan]{settings.Title}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        return Task.FromResult(ExitCode.Success);
    }
}
