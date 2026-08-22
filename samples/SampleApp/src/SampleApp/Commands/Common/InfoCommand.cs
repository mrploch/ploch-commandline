using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Common;

/// <summary>
///     Synchronous command demonstrating <see cref="AppCommand{TSettings}" /> and rich output display.
/// </summary>
public class InfoCommand(ICommandSettingsValidator<InfoCommandSettings> validator,
                         IExceptionHandler exceptionHandler,
                         IOutput output,
                         IConfiguration configuration) : AppCommand<InfoCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext? context, InfoCommandSettings settings, CancellationToken cancellationToken)
    {
        output.MarkupLineInterpolated($"[bold cyan]=== Application & System Information ===[/]");
        output.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Property[/]");
        table.AddColumn("[green]Value[/]");

        table.AddRow("Application Name", "Ploch.CommandLine.Spectre Sample App");
        table.AddRow("Framework", RuntimeInformation.FrameworkDescription);
        table.AddRow("OS Description", RuntimeInformation.OSDescription);
        table.AddRow("Process Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        table.AddRow("Current Directory", Environment.CurrentDirectory);
        table.AddRow("Machine Name", Environment.MachineName);
        table.AddRow("Environment Setting", configuration["SampleAppSettings:Environment"] ?? "Not configured");

        AnsiConsole.Write(table);

        if (settings.ShowDiagnostics)
        {
            output.WriteLine();
            var panel = new Panel(new Markup("[dim]Memory Working Set:[/] [white]" +
                                             (Environment.WorkingSet / 1024 / 1024) + " MB[/]\n" +
                                             "[dim]Thread Count:[/] [white]" + Environment.ProcessorCount + " logical cores[/]\n" +
                                             "[dim]Serilog MinLevel:[/] [white]" + (configuration["Serilog:MinimumLevel:Default"] ?? "Information") + "[/]"))
            {
                Header = new PanelHeader("[bold yellow]Diagnostics[/]"),
                Border = BoxBorder.Double
            };
            AnsiConsole.Write(panel);
        }

        output.WriteLine();
        output.MarkupLineInterpolated($"[green]Command completed successfully.[/]");

        return ExitCode.Success;
    }
}
