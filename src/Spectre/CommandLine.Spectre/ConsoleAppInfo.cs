using Spectre.Console;
using SysColor = System.Drawing.Color;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Represents information about a console application, including its name, description, version, and display settings.
/// </summary>
public class ConsoleAppInfo(params IEnumerable<string>? args) : AppInfo(args)
{
    /// <summary>
    ///     Gets or sets the color used for displaying the application name in FigletText.
    ///     Default is Chartreuse2.
    /// </summary>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification =
                         "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppNameColor { get; set; } = Color.Chartreuse2;

    public SysColor AppNameColorSys { get; set; } = SysColor.FromArgb(Color.Chartreuse2.R, Color.Chartreuse2.G, Color.Chartreuse2.B);

    /// <summary>
    ///     Gets or sets the color used for displaying the application name and version information.
    ///     Default is Wheat1.
    /// </summary>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification =
                         "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppNameInfoColor { get; set; } = Color.Wheat1;

    public SysColor AppNameInfoColorSys { get; set; } = SysColor.FromArgb(Color.Wheat1.R, Color.Wheat1.G, Color.Wheat1.B);

    /// <summary>
    ///     Gets or sets the color used for displaying the application description.
    ///     The default is LightSlateGrey.
    /// </summary>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification =
                         "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppDescriptionColor { get; set; } = Color.LightSlateGrey;

    public SysColor AppDescriptionColorSys { get; set; } = SysColor.FromArgb(Color.LightSlateGrey.R, Color.LightSlateGrey.G, Color.LightSlateGrey.B);

    // /// <summary>
    // ///     Prints the application information to the console, including the name as FigletText,
    // ///     name with a version, and description.
    // /// </summary>
    // /// <exception cref="InvalidOperationException">Thrown when the application name is null or empty.</exception>
    // public void PrintAppInfo()
    // {
    //     Validate();
    //
    //     AnsiConsole.Write(new FigletText(Name!).Color(AppNameColor));
    //
    //     var nameInfoString = Name;
    //     if (Version != null)
    //     {
    //         nameInfoString += $" {Version}";
    //     }
    //
    //     AnsiConsole.MarkupLine($"[{AppNameInfoColor}]{nameInfoString}[/]");
    //
    //     if (!Description!.IsNullOrEmpty())
    //     {
    //         AnsiConsole.MarkupLine($"[{AppDescriptionColor} italic]{Description}[/]");
    //     }
    //
    //     AnsiConsole.WriteLine();
    // }
}
