using Spectre.Console;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Represents information about a console application, including its name, description, version, and display settings.
/// </summary>
public class ConsoleAppInfo(params IEnumerable<string>? args) : AppInfo(args)
{
    /// <summary>
    ///     Gets or sets a value indicating whether the library writes its own lines to standard error: the start-up
    ///     banner — the name as FigletText, the name and version line and the description — when the application is
    ///     built, and the "Executing command" line before each command. Default is <see langword="true" />.
    /// </summary>
    /// <remarks>
    ///     Both go to standard error, so neither mixes with what a command writes to standard output; set this to
    ///     <see langword="false" /> to drop both, for a tool whose standard error is captured too. Read when
    ///     <see cref="AppBuilder.ConfigureCommandApp" /> builds the application.
    /// </remarks>
    public bool ShouldShowBanner { get; set; } = true;

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
