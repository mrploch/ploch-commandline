using System.ComponentModel;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Settings for the config get command.
/// </summary>
public class ConfigGetCommandSettings : CommandSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("The configuration key to retrieve (e.g. 'SampleAppSettings:Environment').")]
    public string Key { get; set; } = string.Empty;
}

/// <summary>
///     Settings for the config set command.
/// </summary>
public class ConfigSetCommandSettings : CommandSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("The configuration key to set.")]
    public string Key { get; set; } = string.Empty;

    [CommandArgument(1, "<VALUE>")]
    [Description("The configuration value to set.")]
    public string Value { get; set; } = string.Empty;

    [CommandOption("-s|--scope <SCOPE>")]
    [Description("The configuration scope ('user' or 'system').")]
    [DefaultValue("user")]
    public string Scope { get; set; } = "user";
}

/// <summary>
///     Settings for the config show command.
/// </summary>
public class ConfigShowCommandSettings : CommandSettings
{
    [CommandOption("-s|--section <SECTION>")]
    [Description("Filter configuration by section name.")]
    public string? Section { get; set; }
}
