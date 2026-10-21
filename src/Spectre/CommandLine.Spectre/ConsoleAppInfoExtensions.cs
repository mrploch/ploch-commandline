using Ploch.Common;
using Ploch.Common.ArgumentChecking;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Provides extension methods for rendering and validating <see cref="ConsoleAppInfo" />.
/// </summary>
public static class ConsoleAppInfoExtensions
{
    /// <summary>
    ///     Prints the application information to the given console, including the name as FigletText,
    ///     name with a version, and description.
    /// </summary>
    /// <param name="appInfo">The application information to print.</param>
    /// <param name="console">
    ///     The console to print to. <see cref="AppBuilder" /> passes its standard error console, so the banner stays out
    ///     of the command's own output; the console remains the caller's.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appInfo" /> or <paramref name="console" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null, empty, or whitespace.</exception>
    public static void PrintAppInfo(this ConsoleAppInfo appInfo, IAnsiConsole console)
    {
        console.NotNull();
        appInfo.Validate();

        console.Write(new FigletText(appInfo.Name!).Color(appInfo.AppNameColor));

        var nameInfoString = appInfo.Name;
        if (appInfo.Version != null)
        {
            nameInfoString += $" {appInfo.Version}";
        }

        // Escaped, not interpolated as markup: the name and version are consumer-supplied, so an application
        // called "Tool [Dev]" would otherwise be parsed as a style tag and throw while rendering its own banner.
        // Markup is honoured where the caller wrote it; escaped where this library adds the tag.
        console.MarkupLine($"[{appInfo.AppNameInfoColor}]{Markup.Escape(nameInfoString ?? string.Empty)}[/]");

        if (!appInfo.Description!.IsNullOrEmpty())
        {
            console.MarkupLine($"[{appInfo.AppDescriptionColor} italic]{Markup.Escape(appInfo.Description!)}[/]");
        }

        console.WriteLine();
    }

    /// <summary>
    ///     Validates that the application information is in a valid state: a name must be present and must not
    ///     consist solely of whitespace, since the banner renders it as FigletText.
    /// </summary>
    /// <param name="appInfo">The application information to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appInfo" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the application name is null, empty, or whitespace.</exception>
    public static void Validate(this ConsoleAppInfo appInfo)
    {
        appInfo.NotNull();

        if (string.IsNullOrWhiteSpace(appInfo.Name))
        {
            throw new InvalidOperationException("Application Name cannot be null, empty, or whitespace.");
        }
    }
}
