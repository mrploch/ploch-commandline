using System.ComponentModel;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Files;

/// <summary>
///     Settings for file processing command, demonstrating <see cref="SupportsTokensAttribute" /> token replacement.
/// </summary>
public class FileProcessCommandSettings : CommandSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("Path to the input file to process.")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("-o|--output-path <PATH>")]
    [Description("Output destination path. Supports tokens like '{date}' and '{datetime}'.")]
    [SupportsTokens]
    [DefaultValue("./processed-{date}/output.dat")]
    public string OutputPath { get; set; } = "./processed-{date}/output.dat";

    [CommandOption("-b|--backup")]
    [Description("Create a backup before processing.")]
    [DefaultValue(true)]
    public bool Backup { get; set; }
}

/// <summary>
///     Settings for file report command.
/// </summary>
public class FileReportCommandSettings : CommandSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("Path to inspect and generate report for.")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("-t|--title <TITLE>")]
    [Description("Report title. Supports tokens like '{date}'.")]
    [SupportsTokens]
    [DefaultValue("File Report - {date}")]
    public string Title { get; set; } = "File Report - {date}";
}
