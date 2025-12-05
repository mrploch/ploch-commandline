using Ploch.Common.ArgumentChecking;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Implements the <see cref="ICommandAppConfigurator" /> interface to configure command-line applications.
/// </summary>
/// <param name="commandApp">The command application to configure.</param>
public class CommandAppConfigurator(ICommandApp commandApp) : ICommandAppConfigurator
{
    /// <summary>
    ///     Configures the command-line application using the provided configuration action.
    /// </summary>
    /// <param name="configuration">An action that configures the command-line application's commands, options, and behaviors.</param>
    /// <returns>An executor that can run the configured command-line application.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is null.</exception>
    public ICommandAppExecutor Configure(Action<IConfigurator> configuration)
    {
        commandApp.Configure(configuration.NotNull());

        return new CommandAppExecutor(commandApp);
    }
}
