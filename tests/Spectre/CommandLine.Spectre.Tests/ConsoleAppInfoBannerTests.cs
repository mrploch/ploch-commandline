using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for <see cref="ConsoleAppInfoExtensions.PrintAppInfo" />, the start-up banner. It renders the name as
///     FigletText exactly once — a second render was the visible symptom of the duplicated-banner defect — then the
///     name/version line and, only when there is one, the description.
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class ConsoleAppInfoBannerTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PrintAppInfo_should_render_the_name_line_and_the_figlet_banner()
    {
        using var console = UseRecordingConsole();

        new ConsoleAppInfo { Name = "Widget" }.PrintAppInfo();

        RenderedLines(console).Should().Contain("Widget", "the plain name line follows the banner");
        RenderedLines(console).Should().HaveCountGreaterThan(3, "the FigletText banner spans several lines above the name line");
    }

    [Fact]
    public void PrintAppInfo_should_append_the_version_to_the_name_line()
    {
        using var console = UseRecordingConsole();

        new ConsoleAppInfo { Name = "Widget", Version = new Version(2, 4, 6) }.PrintAppInfo();

        RenderedLines(console).Should().Contain("Widget 2.4.6");
    }

    [Fact]
    public void PrintAppInfo_should_omit_the_version_when_none_is_set()
    {
        using var console = UseRecordingConsole();

        new ConsoleAppInfo { Name = "Widget" }.PrintAppInfo();

        RenderedLines(console)
            .Should()
            .NotContain(line => line.StartsWith("Widget ", StringComparison.Ordinal), "with no version there is nothing to append to the name");
    }

    [Fact]
    public void PrintAppInfo_should_render_the_description_when_one_is_set()
    {
        using var console = UseRecordingConsole();

        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo();

        RenderedLines(console).Should().Contain("Makes widgets.");
    }

    [Fact]
    public void PrintAppInfo_should_omit_the_description_line_when_there_is_no_description()
    {
        using var withDescription = new RecordingConsole();
        using var withoutDescription = new RecordingConsole();

        AnsiConsole.Console = withDescription.Console;
        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo();

        AnsiConsole.Console = withoutDescription.Console;
        new ConsoleAppInfo { Name = "Widget", Description = string.Empty }.PrintAppInfo();

        RenderedLines(withoutDescription)
            .Should()
            .HaveCount(RenderedLines(withDescription).Count - 1, "the description line is the only difference between the two banners");
    }

    [Fact]
    public void PrintAppInfo_should_render_the_name_only_once_below_the_banner()
    {
        using var console = UseRecordingConsole();

        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo();

        RenderedLines(console).Count(line => line == "Widget").Should().Be(1, "the banner is drawn once, so exactly one plain name line follows it");
    }

    [Fact]
    public void PrintAppInfo_should_reject_an_application_without_a_name()
    {
        using var console = UseRecordingConsole();
        var appInfo = new ConsoleAppInfo { Name = "  " };

        Action act = appInfo.PrintAppInfo;

        act.Should().Throw<InvalidOperationException>();
        console.Output.Should().BeEmpty("validation runs before anything is rendered");
    }

    /// <summary>Splits the captured output into trimmed, non-blank lines so assertions ignore Spectre's padding.</summary>
    private static List<string> RenderedLines(RecordingConsole console) =>
        console.Output.Split(Environment.NewLine).Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();

    private static RecordingConsole UseRecordingConsole()
    {
        var console = new RecordingConsole();
        AnsiConsole.Console = console.Console;

        return console;
    }
}
