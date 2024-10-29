using McMaster.Extensions.CommandLineUtils;

namespace Ploch.Common.CommandLine;

/// <summary>
///     Provides extension methods for the CommandLineApplication class.
/// </summary>
public static class CommandLineApplicationRuntimeExtensions
{
    /// <summary>
    ///     Writes the help text for the specified command line application.
    /// </summary>
    /// <param name="app">The command line application instance.</param>
    /// <returns>The same command line application instance.</returns>
    public static CommandLineApplication WriteHelpText(this CommandLineApplication app)
    {
        app.HelpTextGenerator.Generate(app, app.Out);
        return app;
    }
}