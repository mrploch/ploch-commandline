using System.ComponentModel;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Common;

/// <summary>
///     Options shared by every command in the <c>user</c> branch. Settings classes inherit from a common
///     base so an option is declared once and appears in the help of each command that derives from it.
/// </summary>
public class GlobalSettings : CommandSettings
{
    [CommandOption("-v|--verbose")]
    [Description("Enable verbose console output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
}

/// <summary>
///     Settings for the info command.
/// </summary>
public class InfoCommandSettings : CommandSettings
{
    [CommandOption("-d|--diagnostics")]
    [Description("Display extended runtime and host diagnostics.")]
    [DefaultValue(false)]
    public bool ShowDiagnostics { get; set; }
}
