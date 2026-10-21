using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for <see cref="ConsoleAppInfoExtensions.PrintAppInfo" />, the start-up banner. It renders the name as
///     FigletText exactly once — a second render was the visible symptom of the duplicated-banner defect — then the
///     name/version line and, only when there is one, the description, all on the console it is given and nothing on
///     the global one (PLO-783: the banner goes to standard error, not standard output).
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class ConsoleAppInfoBannerTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly RecordingConsole _console = new();

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _console.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PrintAppInfo_should_render_the_name_line_and_the_figlet_banner()
    {
        new ConsoleAppInfo { Name = "Widget" }.PrintAppInfo(_console.Console);

        RenderedLines(_console).Should().Contain("Widget", "the plain name line follows the banner");
        RenderedLines(_console).Should().HaveCountGreaterThan(3, "the FigletText banner spans several lines above the name line");
    }

    /// <summary>
    ///     The banner is written to the console it is given and to nothing else: the global console is standard
    ///     output, and the whole point of taking a console is to keep the banner off it.
    /// </summary>
    [Fact]
    public void PrintAppInfo_should_write_nothing_to_the_global_console()
    {
        using var globalConsole = new RecordingConsole();
        AnsiConsole.Console = globalConsole.Console;

        new ConsoleAppInfo { Name = "Widget", Version = new Version(1, 0), Description = "Makes widgets." }.PrintAppInfo(_console.Console);

        globalConsole.Output.Should().BeEmpty("the banner belongs to the console it was given, never to standard output");
        _console.Output.Should().Contain("Widget 1.0");
    }

    [Fact]
    public void PrintAppInfo_should_reject_null_app_info()
    {
        var act = () => ((ConsoleAppInfo)null!).PrintAppInfo(_console.Console);

        act.Should().Throw<ArgumentNullException>().WithParameterName("appInfo");
        _console.Output.Should().BeEmpty();
    }

    [Fact]
    public void PrintAppInfo_should_reject_a_null_console()
    {
        var act = () => new ConsoleAppInfo { Name = "Widget" }.PrintAppInfo(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("console");
    }

    /// <summary>
    ///     The name and version are consumer-supplied and were interpolated straight into a markup string, so an
    ///     application called "Widget [Dev]" threw while rendering its own start-up banner. Markup is honoured where
    ///     the caller wrote it and escaped where this library adds the tag, so the brackets must survive as text.
    /// </summary>
    [Fact]
    public void PrintAppInfo_should_not_parse_the_application_name_as_markup()
    {
        var act = () => new ConsoleAppInfo { Name = "Widget [Dev]" }.PrintAppInfo(_console.Console);

        act.Should().NotThrow("consumer metadata must not be parsed as a style tag");
        RenderedLines(_console).Should().Contain("Widget [Dev]", "the brackets belong to the name, not to the markup");
    }

    [Fact]
    public void PrintAppInfo_should_not_parse_the_description_as_markup()
    {
        var act = () => new ConsoleAppInfo { Name = "Widget", Description = "Handles [square] brackets" }.PrintAppInfo(_console.Console);

        act.Should().NotThrow();
        RenderedLines(_console).Should().Contain("Handles [square] brackets");
    }

    [Fact]
    public void PrintAppInfo_should_append_the_version_to_the_name_line()
    {
        new ConsoleAppInfo { Name = "Widget", Version = new Version(2, 4, 6) }.PrintAppInfo(_console.Console);

        RenderedLines(_console).Should().Contain("Widget 2.4.6");
    }

    [Fact]
    public void PrintAppInfo_should_omit_the_version_when_none_is_set()
    {
        new ConsoleAppInfo { Name = "Widget" }.PrintAppInfo(_console.Console);

        RenderedLines(_console)
            .Should()
            .NotContain(line => line.StartsWith("Widget ", StringComparison.Ordinal), "with no version there is nothing to append to the name");
    }

    [Fact]
    public void PrintAppInfo_should_render_the_description_when_one_is_set()
    {
        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo(_console.Console);

        RenderedLines(_console).Should().Contain("Makes widgets.");
    }

    [Fact]
    public void PrintAppInfo_should_omit_the_description_line_when_there_is_no_description()
    {
        using var withoutDescription = new RecordingConsole();

        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo(_console.Console);
        new ConsoleAppInfo { Name = "Widget", Description = string.Empty }.PrintAppInfo(withoutDescription.Console);

        RenderedLines(withoutDescription)
            .Should()
            .HaveCount(RenderedLines(_console).Count - 1, "the description line is the only difference between the two banners");
    }

    [Fact]
    public void PrintAppInfo_should_render_the_name_only_once_below_the_banner()
    {
        new ConsoleAppInfo { Name = "Widget", Description = "Makes widgets." }.PrintAppInfo(_console.Console);

        RenderedLines(_console).Count(line => line == "Widget").Should().Be(1, "the banner is drawn once, so exactly one plain name line follows it");
    }

    [Fact]
    public void PrintAppInfo_should_reject_an_application_without_a_name()
    {
        var appInfo = new ConsoleAppInfo { Name = "  " };

        var act = () => appInfo.PrintAppInfo(_console.Console);

        act.Should().Throw<InvalidOperationException>();
        _console.Output.Should().BeEmpty("validation runs before anything is rendered");
    }

    /// <summary>Splits the captured output into trimmed, non-blank lines so assertions ignore Spectre's padding.</summary>
    private static List<string> RenderedLines(RecordingConsole console) =>
        console.Output.Split(Environment.NewLine).Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();
}
