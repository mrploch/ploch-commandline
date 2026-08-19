using Ploch.Common;
using Spectre.Console;
using SysColor = System.Drawing.Color;

namespace Ploch.CommandLine.Spectre;

public static class ConsoleAppInfoExtensions
{
    /// <summary>
    ///     Prints the application information to the console, including the name as FigletText,
    ///     name with a version, and description.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null or empty.</exception>
    public static void PrintAppInfo(this ConsoleAppInfo appInfo)
    {
        appInfo.Validate();

        AnsiConsole.Write(new FigletText(appInfo.Name!).Color(appInfo.AppNameColor));
        AnsiConsole.Write(new FigletText(appInfo.Name!).Color(appInfo.AppNameColorSys.FromSysColor()));

        var nameInfoString = appInfo.Name;
        if (appInfo.Version != null)
        {
            nameInfoString += $" {appInfo.Version}";
        }

        AnsiConsole.MarkupLine($"[{appInfo.AppNameInfoColor}]{nameInfoString}[/]");

        if (!appInfo.Description!.IsNullOrEmpty())
        {
            AnsiConsole.MarkupLine($"[{appInfo.AppDescriptionColor} italic]{appInfo.Description}[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    ///     Validates that the application information is in a valid state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null or empty.</exception>
    public static void Validate(this ConsoleAppInfo appInfo)
    {
        if (appInfo.Name!.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Application Name cannot be null.");
        }
    }

    public static Color FromSysColor(this SysColor color) => new(color.R, color.G, color.B);
}
