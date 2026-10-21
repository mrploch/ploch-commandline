using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Creates the <see cref="IAnsiConsole" /> the library writes its own diagnostics to: the start-up banner and the
///     "Executing command" line go to standard error, so that what a command renders through <see cref="IOutput" />
///     is all that reaches standard output and can be piped on its own.
/// </summary>
internal static class StandardErrorConsole
{
    /// <summary>
    ///     Creates an <see cref="IAnsiConsole" /> over <see cref="Console.Error" />: with Spectre's own detection of
    ///     colour and ANSI support when standard error is a terminal, and plain text when it is redirected.
    /// </summary>
    /// <returns>A console writing to the process's standard error stream.</returns>
    public static IAnsiConsole Create() => Create(Console.IsErrorRedirected);

    /// <summary>
    ///     Creates an <see cref="IAnsiConsole" /> over <see cref="Console.Error" />, plain when standard error is
    ///     redirected and with Spectre's own detection otherwise.
    /// </summary>
    /// <param name="errorRedirected">
    ///     Whether standard error is redirected — to a file, a pipe or a capturing parent process — rather than a
    ///     terminal. Normally <see cref="Console.IsErrorRedirected" />; a parameter so a test can cover both paths.
    /// </param>
    /// <returns>A console writing to the process's standard error stream.</returns>
    /// <remarks>
    ///     Spectre's detection answers "does this environment understand ANSI?" from <c>TERM</c> on Unix, whatever the
    ///     stream points at, and its profile enrichers switch ANSI back on under a CI service such as GitHub Actions
    ///     after the settings have been applied, so a captured standard error would still receive escape sequences.
    ///     Redirection is checked here instead, and the enrichers are left out on that path: a terminal keeps colour
    ///     and markup, a capture gets the plain text a reader expects.
    /// </remarks>
    public static IAnsiConsole Create(bool errorRedirected) =>
        AnsiConsole.Create(new AnsiConsoleSettings
                           {
                               Ansi = errorRedirected ? AnsiSupport.No : AnsiSupport.Detect,
                               ColorSystem = errorRedirected ? ColorSystemSupport.NoColors : ColorSystemSupport.Detect,
                               Enrichment = new ProfileEnrichment { UseDefaultEnrichers = !errorRedirected },
                               Out = new AnsiConsoleOutput(Console.Error)
                           });
}
