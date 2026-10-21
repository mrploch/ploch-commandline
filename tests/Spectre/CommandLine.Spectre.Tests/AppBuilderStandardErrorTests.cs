using System.Globalization;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Proves the default construction paths really route the banner and the "Executing command" line to the
///     process's standard error (PLO-783). The injected-console tests in <see cref="AppBuilderTests" /> verify the
///     behaviour; these verify the wiring behind <see cref="AppBuilder.Create" />, the public constructor and
///     <see cref="StandardErrorConsole" />, by redirecting <see cref="Console.Error" /> for the duration of a test.
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class AppBuilderStandardErrorTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly TextWriter _originalError = Console.Error;
    private readonly RecordingConsole _stdout = new();
    private readonly StringWriter _stderr = new(CultureInfo.InvariantCulture);

    public AppBuilderStandardErrorTests()
    {
        AnsiConsole.Console = _stdout.Console;
        Console.SetError(_stderr);
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        AnsiConsole.Console = _originalConsole;
        EnvironmentSettings.Reset();
        _stdout.Dispose();
        _stderr.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StandardErrorConsole_Create_should_write_to_the_process_standard_error()
    {
        var console = StandardErrorConsole.Create();

        console.WriteLine("to standard error");

        StandardError().Should().Contain("to standard error");
        _stdout.Output.Should().BeEmpty();
    }

    /// <summary>
    ///     Spectre's own ANSI detection reads <c>TERM</c>, not the stream, so a captured standard error would still
    ///     receive escape sequences. The raw output is asserted here on purpose: stripping escapes first would hide it.
    /// </summary>
    [Fact]
    public void StandardErrorConsole_Create_should_write_plain_text_when_standard_error_is_redirected()
    {
        var console = StandardErrorConsole.Create(errorRedirected: true);

        console.MarkupLine("[bold red]plain[/]");

        _stderr.ToString().Should().Be("plain" + Environment.NewLine, "a capture expects the text a reader sees, with no escape sequences");
    }

    /// <summary>
    ///     The non-redirected path leaves colour and ANSI support to Spectre's detection, which depends on the
    ///     environment the tests run in, so only what is invariant is asserted: the visible text survives whatever the
    ///     detection decided.
    /// </summary>
    [Fact]
    public void StandardErrorConsole_Create_should_keep_the_visible_text_whatever_the_detected_capabilities_when_standard_error_is_not_redirected()
    {
        var console = StandardErrorConsole.Create(errorRedirected: false);

        console.MarkupLine("[bold red]styled[/]");

        StandardError().Should().Be("styled" + Environment.NewLine);
    }

    [Fact]
    public void Create_should_write_the_banner_and_the_executing_line_to_standard_error_and_nothing_to_standard_output()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool").WithVersion(new Version(4, 5));

        var exitCode = builder.ConfigureCommandApp(configurator =>
                                              {
                                                  // Spectre renders its own errors on a console it memoises process-wide; this test's console keeps
                                                  // that rendering visible here and off a console an earlier test has disposed.
                                                  configurator.ConfigureConsole(_stdout.Console);
                                                  configurator.AddCommand<EchoCommand>("echo");
                                              })
                                   .Run("echo");

        exitCode.Should().Be(0);
        StandardError().Should().Contain("Widget Tool 4.5").And.Contain("Executing command echo");
        _stdout.Output.Should().Be("echoed" + Environment.NewLine, "standard output carries the command's output and nothing else");
    }

    [Fact]
    public void Constructor_should_write_the_banner_and_the_executing_line_to_standard_error_and_nothing_to_standard_output()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Widget Tool" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                                              {
                                                  // Spectre renders its own errors on a console it memoises process-wide; this test's console keeps
                                                  // that rendering visible here and off a console an earlier test has disposed.
                                                  configurator.ConfigureConsole(_stdout.Console);
                                                  configurator.AddCommand<EchoCommand>("echo");
                                              })
                                   .Run("echo");

        exitCode.Should().Be(0);
        StandardError().Should().Contain("Widget Tool").And.Contain("Executing command echo");
        _stdout.Output.Should().Be("echoed" + Environment.NewLine);
    }

    [Fact]
    public void Create_should_write_nothing_to_standard_error_when_the_banner_is_off()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool").ShowBanner(false);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                                              {
                                                  // Spectre renders its own errors on a console it memoises process-wide; this test's console keeps
                                                  // that rendering visible here and off a console an earlier test has disposed.
                                                  configurator.ConfigureConsole(_stdout.Console);
                                                  configurator.AddCommand<EchoCommand>("echo");
                                              })
                                   .Run("echo");

        exitCode.Should().Be(0);
        StandardError().Should().BeEmpty("the switch silences the banner and the announcement alike");
        _stdout.Output.Should().Be("echoed" + Environment.NewLine);
    }

    /// <summary>The visible text written to standard error, with any ANSI escape sequences removed.</summary>
    private string StandardError() => RecordingConsole.StripEscapeSequences(_stderr.ToString());

    private sealed class EchoSettings : CommandSettings
    {
    }

    private sealed class EchoCommand(IAnsiConsole console) : Command<EchoSettings>
    {
        public override int Execute(CommandContext context, EchoSettings settings, CancellationToken cancellationToken)
        {
            console.WriteLine("echoed");

            return 0;
        }
    }
}
