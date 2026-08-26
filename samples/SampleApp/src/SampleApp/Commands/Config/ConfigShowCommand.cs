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

    /// <summary>
    ///     Key fragments whose values are redacted wherever they appear beneath an allowed section.
    /// </summary>
    /// <remarks>
    ///     The section allow-list above keeps whole trees out, but it does not make the leaves inside them safe. The
    ///     environment provider can populate any key beneath an allowed section - Serilog__WriteTo__0__Args__apiKey
    ///     is a real example - and the recursion below would print it verbatim. Matching on the full path rather than
    ///     the leaf key catches ConnectionStrings:Default as well as Args:apiKey.
    ///     Name matching is a heuristic, not a guarantee: it is the right shape for a sample, but a real application
    ///     handling secrets should keep them out of renderable configuration in the first place.
    /// </remarks>
    private static readonly string[] SensitivePathFragments =
        ["password", "pwd", "secret", "token", "apikey", "api_key", "credential", "connectionstring", "privatekey", "accesskey"];

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
            if (IsSensitive(section.Path))
            {
                parentNode.AddNode("[dim]Value:[/] [yellow]<redacted>[/]");

                return;
            }

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

    private static bool IsSensitive(string path) =>
        SensitivePathFragments.Any(fragment => path.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
