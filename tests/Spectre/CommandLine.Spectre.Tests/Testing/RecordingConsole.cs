using System.Globalization;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests.Testing;

/// <summary>
///     An <see cref="IAnsiConsole" /> backed by a <see cref="StringWriter" />, so a test can assert on what was
///     actually rendered. Colours and ANSI escapes are switched off, which means the captured text is the plain
///     text a reader would see — markup that Spectre parsed is applied and then stripped, markup that Spectre
///     could not parse would surface as an exception rather than as literal brackets.
/// </summary>
internal sealed class RecordingConsole : IDisposable
{
    private readonly StringWriter _writer = new(CultureInfo.InvariantCulture);

    public RecordingConsole()
    {
        Console = AnsiConsole.Create(new AnsiConsoleSettings
                                     {
                                         Ansi = AnsiSupport.No,
                                         ColorSystem = ColorSystemSupport.NoColors,
                                         Interactive = InteractionSupport.No,
                                         Out = new AnsiConsoleOutput(_writer)
                                     });

        // A narrow profile would wrap the rendered text and break substring assertions.
        Console.Profile.Width = 500;
        Console.Profile.Height = 500;
    }

    public IAnsiConsole Console { get; }

    /// <summary>Gets everything written so far, exactly as rendered.</summary>
    public string Output => _writer.ToString();

    /// <summary>Gets the rendered text with line breaks and padding collapsed, for whitespace-insensitive assertions.</summary>
    public string FlatOutput => string.Join(" ", Output.Split('\n', '\r', ' ').Where(part => part.Length > 0));

    public void Dispose() => _writer.Dispose();
}
