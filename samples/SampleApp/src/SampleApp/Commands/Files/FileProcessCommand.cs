using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Files;

/// <summary>
///     Command demonstrating token replacement via <see cref="SupportsTokensAttribute" /> and progress simulation.
/// </summary>
public class FileProcessCommand(CommandArgumentsRootProcessor settingsProcessor,
                                ICommandSettingsValidator<FileProcessCommandSettings> validator,
                                IExceptionHandler exceptionHandler,
                                IOutput output) : AsyncAppCommand<FileProcessCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, FileProcessCommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[bold cyan]Processing File:[/] [yellow]{settings.Path}[/]");
        Output.MarkupLineInterpolated($"[bold cyan]Resolved Output Path (with tokens replaced):[/] [green]{settings.OutputPath}[/]");
        Output.MarkupLineInterpolated($"[dim]Backup enabled: {settings.Backup}[/]");
        Output.WriteLine();

        await AnsiConsole.Status()
                         .Spinner(Spinner.Known.Dots)
                         .StartAsync("Processing file content...",
                                     async _ =>
                                     {
                                         // Simulated work: the token is honoured so Ctrl+C stops the command
                                         // instead of waiting for the delay to elapse.
                                         await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
                                     });

        Output.MarkupLineInterpolated($"[bold green]File processed successfully![/]");
        Output.MarkupLineInterpolated($"Saved result to: [underline]{settings.OutputPath}[/]");

        return ExitCode.Success;
    }
}
