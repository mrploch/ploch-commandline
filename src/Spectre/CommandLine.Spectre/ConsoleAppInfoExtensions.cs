using Ploch.Common;
using Spectre.Console;
using SysColor = System.Drawing.Color;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Provides extension methods for rendering and validating <see cref="ConsoleAppInfo" />.
/// </summary>
public static class ConsoleAppInfoExtensions
{
    /// <summary>
    ///     Prints the application information to the console, including the name as FigletText,
    ///     name with a version, and description.
    /// </summary>
    /// <param name="appInfo">The application information to print.</param>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null or empty.</exception>
    public static void PrintAppInfo(this ConsoleAppInfo appInfo)
    {
        appInfo.Validate();

        AnsiConsole.Write(new FigletText(appInfo.Name!).Color(appInfo.AppNameColor));

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
    /// <param name="appInfo">The application information to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null or empty.</exception>
    public static void Validate(this ConsoleAppInfo appInfo)
    {
        if (appInfo.Name!.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Application Name cannot be null.");
        }
    }

    /// <summary>
    ///     Converts a <see cref="SysColor" /> to the Spectre.Console <see cref="Color" /> equivalent.
    /// </summary>
    /// <param name="color">The system colour to convert.</param>
    /// <returns>A <see cref="Color" /> with the same red, green, and blue components.</returns>
    public static Color FromSysColor(this SysColor color) => new(color.R, color.G, color.B);
}
