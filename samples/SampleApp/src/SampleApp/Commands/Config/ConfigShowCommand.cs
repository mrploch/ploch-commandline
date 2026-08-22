using Microsoft.Extensions.Configuration;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Command to display the application's own configuration, formatted as a tree.
/// </summary>
public class ConfigShowCommand(ICommandSettingsValidator<ConfigShowCommandSettings> validator,
                               IExceptionHandler exceptionHandler,
                               IOutput output,
                               IConfiguration configuration) : AppCommand<ConfigShowCommandSettings>(validator, exceptionHandler)
{
    /// <summary>
    ///     The configuration sections this application owns.
    /// </summary>
    /// <remarks>
    ///     Deliberately an allow-list rather than <c>configuration.GetChildren()</c>. The host adds an
    ///     environment-variable configuration provider, so enumerating the configuration root would print
    ///     every environment variable of the process — API keys and access tokens included. A CLI that
    ///     renders configuration must show only the keys it owns.
    /// </remarks>
    private static readonly string[] ApplicationSections = ["SampleAppSettings", "Logging", "Serilog"];

    /// <inheritdoc />
    protected override ExitCode DoExecute(CommandContext? context, ConfigShowCommandSettings settings, CancellationToken cancellationToken)
    {
        output.MarkupLineInterpolated($"[bold cyan]Application Configuration Settings[/]");
        output.WriteLine();

        var root = new Tree("[bold yellow]Configuration[/]");
        var rendered = 0;

        foreach (var sectionName in ApplicationSections)
        {
            if (settings.Section is not null && !sectionName.Contains(settings.Section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                continue;
            }

            var sectionNode = root.AddNode($"[cyan]{Markup.Escape(section.Key)}[/]");
            AddSectionChildren(sectionNode, section);
            rendered++;
        }

        if (rendered == 0)
        {
            output.MarkupLineInterpolated($"[yellow]No configuration section matched '{settings.Section}'.[/]");
            output.MarkupLineInterpolated($"[dim]Known sections: {string.Join(", ", ApplicationSections)}[/]");

            return ExitCode.InvalidInput;
        }

        output.Write(root);

        return ExitCode.Success;
    }

    private static void AddSectionChildren(TreeNode parentNode, IConfigurationSection section)
    {
        var children = section.GetChildren().ToList();
        if (children.Count == 0 && section.Value != null)
        {
            // Keys and values come from JSON and the environment: escape them so a value
            // containing '[' renders as text instead of being parsed as markup.
            parentNode.AddNode($"[dim]Value:[/] [green]{Markup.Escape(section.Value)}[/]");

            return;
        }

        foreach (var child in children)
        {
            var childNode = parentNode.AddNode($"[white]{Markup.Escape(child.Key)}[/]");
            AddSectionChildren(childNode, child);
        }
    }
}
