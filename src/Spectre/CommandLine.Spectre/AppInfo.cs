using Ploch.Common;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre;

public class AppInfo
{
    public AppInfo(params IEnumerable<string>? args)
    {
        Args = args;
    }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public Version? Version { get; set; }

    public IEnumerable<string>? Args { get; }

    public Color AppNameColor { get; set; } = Color.Chartreuse2;

    public Color AppNameInfoColor { get; set; } = Color.Wheat1;

    public Color AppDescriptionColor { get; set; } = Color.LightSlateGrey;

    public void PrintAppInfo()
    {
        Validate();

        AnsiConsole.Write(new FigletText(Name!).Color(AppNameColor));

        var nameInfoString = Name;
        if (Version != null)
        {
            nameInfoString += $" {Version}";
        }

        AnsiConsole.MarkupLine($"[{AppNameInfoColor}]{nameInfoString}[/]");

        if (!Description!.IsNullOrEmpty())
        {
            AnsiConsole.MarkupLine($"[{AppDescriptionColor} italic]{Description}[/]");
        }

        AnsiConsole.WriteLine();
    }

    public void Validate()
    {
        if (Name!.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Application Name cannot be null.");
        }
    }
}