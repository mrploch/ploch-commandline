using Spectre.Console;

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
                     Justification = "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppNameColor { get; set; } = Color.Chartreuse2;

    /// <summary>
    ///     Gets or sets the color used for displaying the application name and version information.
    ///     Default is Wheat1.
    /// </summary>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification = "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppNameInfoColor { get; set; } = Color.Wheat1;

    /// <summary>
    ///     Gets or sets the color used for displaying the application description.
    ///     The default is LightSlateGrey.
    /// </summary>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification = "This property is a part of the public API and may be used by consumers to customize the appearance of the application name.")]
    public Color AppDescriptionColor { get; set; } = Color.LightSlateGrey;
}
