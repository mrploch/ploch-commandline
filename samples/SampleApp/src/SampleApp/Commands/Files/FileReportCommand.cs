using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Files;

/// <summary>
///     Command demonstrating report generation for a file, input validation and markup escaping.
/// </summary>
public class FileReportCommand(CommandArgumentsRootProcessor settingsProcessor,
                               ICommandSettingsValidator<FileReportCommandSettings> validator,
                               IExceptionHandler exceptionHandler,
                               IOutput output) : AsyncAppCommand<FileReportCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    /// <inheritdoc />
    protected override Task<ExitCode> DoExecuteAsync(CommandContext context, FileReportCommandSettings settings, CancellationToken cancellationToken)
    {
        // A path that parses is not a path that exists: report the real state of the input rather
        // than declaring success for a file that was never opened.
        var file = new FileInfo(settings.Path);
        if (!file.Exists)
        {
            Output.MarkupLineInterpolated($"[red]File '{settings.Path}' was not found.[/]");

            return Task.FromResult(ExitCode.InvalidInput);
        }

        // Title and path come from the command line: escape them before they are parsed as markup,
        // otherwise a '[' in either one breaks the render or injects formatting.
        var title = Markup.Escape(settings.Title);
        var path = Markup.Escape(file.FullName);

        var panel = new Panel(new Markup($"[bold]Report Title:[/] {title}\n" +
                                         $"[bold]Source File:[/] {path}\n" +
                                         $"[bold]Size:[/] {file.Length} bytes\n" +
                                         $"[bold]Last Modified:[/] {file.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                         $"[bold]Status:[/] [green]Analysed[/]\n" +
                                         $"[bold]Generated:[/] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"))
        {
            Header = new PanelHeader($"[bold cyan]{title}[/]"),
            Border = BoxBorder.Rounded
        };

        Output.Write(panel);

        return Task.FromResult(ExitCode.Success);
    }
}
