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
public class InfoCommand(CommandArgumentsRootProcessor settingsProcessor,
                         ICommandSettingsValidator<InfoCommandSettings> validator,
                         IExceptionHandler exceptionHandler,
                         IOutput output,
                         IConfiguration configuration) : AppCommand<InfoCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    /// <inheritdoc />
    protected override ExitCode DoExecute(CommandContext? context, InfoCommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[bold cyan]=== Application & System Information ===[/]");
        Output.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Property[/]");
        table.AddColumn("[green]Value[/]");

        // Table cells are parsed as markup, so every value that did not come from this source file
        // is escaped. A path or machine name containing '[' would otherwise break the render.
        table.AddRow("Application Name", "Ploch.CommandLine.Spectre Sample App");
        table.AddRow("Framework", Markup.Escape(RuntimeInformation.FrameworkDescription));
        table.AddRow("OS Description", Markup.Escape(RuntimeInformation.OSDescription));
        table.AddRow("Process Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        table.AddRow("Current Directory", Markup.Escape(Environment.CurrentDirectory));
        table.AddRow("Machine Name", Markup.Escape(Environment.MachineName));
        table.AddRow("Environment Setting", Markup.Escape(configuration["SampleAppSettings:Environment"] ?? "Not configured"));

        // Rendered through IOutput rather than the static AnsiConsole, so the command stays testable
        // with a mocked IOutput and honours whatever console the host configured.
        Output.Write(table);

        if (settings.ShowDiagnostics)
        {
            Output.WriteLine();
            var serilogLevel = Markup.Escape(configuration["Serilog:MinimumLevel:Default"] ?? "Information");
            var panel = new Panel(new Markup($"[dim]Memory Working Set:[/] [white]{Environment.WorkingSet / 1024 / 1024} MB[/]\n" +
                                             $"[dim]Thread Count:[/] [white]{Environment.ProcessorCount} logical cores[/]\n" +
                                             $"[dim]Serilog MinLevel:[/] [white]{serilogLevel}[/]"))
            {
                Header = new PanelHeader("[bold yellow]Diagnostics[/]"),
                Border = BoxBorder.Double
            };
            Output.Write(panel);
        }

        Output.WriteLine();
        Output.MarkupLineInterpolated($"[green]Command completed successfully.[/]");

        return ExitCode.Success;
    }
}
