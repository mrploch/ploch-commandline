using System.Globalization;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests.Testing;

/// <summary>
///     An <see cref="IAnsiConsole" /> backed by a <see cref="StringWriter" />, so a test can assert on what was
///     actually rendered. Colours are switched off and escape sequences are stripped when the output is read, which
///     leaves the plain text a reader would see: markup that Spectre parsed is applied and then removed, while
///     markup Spectre could not parse would surface as an exception rather than as literal brackets.
/// </summary>
/// <remarks>
///     ANSI is switched on deliberately rather than off. With it off, Spectre falls back to the console API on
///     Windows and still emits escape sequences for decorations such as bold and italic elsewhere, so the captured
///     text differs between a developer machine and the Linux CI runner. Turning it on and stripping the sequences
///     when the output is read gives the same visible text everywhere.
/// </remarks>
internal sealed class RecordingConsole : IDisposable
{
    private const char Escape = (char)27;

    /// <summary>
    ///     Matches a CSI escape sequence (styles, cursor movement) or an OSC one (hyperlinks, terminated by BEL or
    ///     ST). The escape character comes from its code point, so none appears in this file.
    /// </summary>
    private static readonly Regex EscapeSequence = new(Escape + @"(?:\[[0-9;?]*[ -/]*[@-~]|\][^\u0007\u001b]*(?:\u0007|\u001b\\))", RegexOptions.Compiled);

    private readonly StringWriter _writer = new(CultureInfo.InvariantCulture);

    public RecordingConsole()
    {
        Console = AnsiConsole.Create(new AnsiConsoleSettings
                                     {
                                         Ansi = AnsiSupport.Yes,
                                         ColorSystem = ColorSystemSupport.NoColors,
                                         Interactive = InteractionSupport.No,
                                         Out = new AnsiConsoleOutput(_writer)
                                     });

        // A narrow profile would wrap the rendered text and break substring assertions.
        Console.Profile.Width = 500;
        Console.Profile.Height = 500;
    }

    public IAnsiConsole Console { get; }

    /// <summary>Gets the visible text written so far, with any ANSI escape sequences removed.</summary>
    public string Output => StripEscapeSequences(RawOutput);

    /// <summary>Gets everything written so far, escape sequences included.</summary>
    public string RawOutput => _writer.ToString();

    /// <summary>Removes any ANSI escape sequences from <paramref name="text" />, leaving the visible text.</summary>
    /// <param name="text">Console output, possibly carrying escape sequences.</param>
    /// <returns>The text a reader would see.</returns>
    public static string StripEscapeSequences(string text) => EscapeSequence.Replace(text, string.Empty);

    public void Dispose() => _writer.Dispose();
}
