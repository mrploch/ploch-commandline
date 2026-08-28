using System.ComponentModel;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Common;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users;

/// <summary>
///     Settings for the user add command.
/// </summary>
public class UserAddCommandSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The full name of the user.")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-e|--email <EMAIL>")]
    [Description("The email address of the user.")]
    public string Email { get; set; } = string.Empty;

    [CommandOption("-r|--role <ROLE>")]
    [Description("The role of the user (e.g. Administrator, Developer, Viewer).")]
    [DefaultValue("Developer")]
    public string Role { get; set; } = "Developer";
}

/// <summary>
///     Settings for the user list command.
/// </summary>
public class UserListCommandSettings : GlobalSettings
{
    [CommandOption("-a|--active-only")]
    [Description("Only display active user accounts.")]
    [DefaultValue(false)]
    public bool ActiveOnly { get; set; }

    [CommandOption("-f|--format <FORMAT>")]
    [Description("The output format: 'table' or 'compact'.")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";
}

/// <summary>
///     Settings for the user delete command.
/// </summary>
public class UserDeleteCommandSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("The numeric ID of the user to delete.")]
    public int Id { get; set; }

    [CommandOption("--force")]
    [Description("Force deletion without confirmation.")]
    [DefaultValue(false)]
    public bool Force { get; set; }
}
