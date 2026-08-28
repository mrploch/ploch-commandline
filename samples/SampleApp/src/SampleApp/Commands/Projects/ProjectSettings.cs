using System.ComponentModel;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects;

/// <summary>
///     Settings for creating a project.
/// </summary>
public class ProjectCreateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The unique name of the project.")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-d|--description <DESC>")]
    [Description("The description of the project.")]
    [DefaultValue("Sample project")]
    public string Description { get; set; } = "Sample project";

    [CommandOption("-t|--template <TEMPLATE>")]
    [Description("The project template (e.g. Console, WebAPI, Library).")]
    [DefaultValue("Console")]
    public string Template { get; set; } = "Console";
}

/// <summary>
///     Settings for exporting a project.
/// </summary>
public class ProjectExportCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The name of the project to export.")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-o|--output <PATH>")]
    [Description("Destination output directory. Supports '{date}' token.")]
    [SupportsTokens]
    [DefaultValue("./exports-{date}")]
    public string OutputPath { get; set; } = "./exports-{date}";
}
