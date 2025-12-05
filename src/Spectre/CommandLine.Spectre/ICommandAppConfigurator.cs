using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
/// Defines an interface for configuring command-line applications.
/// </summary>
public interface ICommandAppConfigurator
{
    /// <summary>
    /// Configures the command-line application using the provided configuration action.
    /// </summary>
    /// <param name="configuration">An action that configures the command-line application's commands, options, and behaviors.</param>
    /// <returns>An executor that can run the configured command-line application.</returns>
    ICommandAppExecutor Configure(Action<IConfigurator> configuration);
}
