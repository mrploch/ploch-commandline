using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Serilog;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Configuration;

/// <summary>
///     A services bundle that configures essential services for a Spectre.Console-based command-line application.
/// </summary>
/// <remarks>
///     This bundle registers Spectre.Console components, logging services, and command-related handlers
///     to provide a complete foundation for command-line applications.
/// </remarks>
public class AppServicesBundle : ConfigurableServicesBundle
{
    /// <summary>
    ///     Gets the bundles registered before this one — Serilog configuration and the output services.
    /// </summary>
    protected override IEnumerable<IServicesBundle>? Dependencies => [ new SerilogConfigurationBundle(), new OutputServicesBundle() ];

    /// <summary>
    ///     Configures the service collection with required services for a Spectre.Console-based command-line application.
    /// </summary>
    /// <param name="configuration">The configuration supplied to the bundle.</param>
    /// <remarks>
    ///     This method registers:
    ///     - Spectre.Console input, cursor, exclusivity mode, and profile as singletons
    ///     - CommandArgumentsRootProcessor and the TokensArgumentsProcessor implementation of ICommandSettingsProcessor
    ///       as transient services, so each resolution receives a fresh instance
    ///     - Command settings validation and exception handling services as singletons
    ///     - Logging services.
    /// </remarks>
    protected override void Configure(IConfiguration configuration)
    {
        // The console singleton comes from OutputServicesBundle, which is a declared dependency of this
        // bundle. Registering it here as well produced a duplicate singleton descriptor.
        Services.AddSingleton(AnsiConsole.Console.Input)
                .AddSingleton(AnsiConsole.Console.Cursor)
                .AddSingleton(AnsiConsole.Console.ExclusivityMode)
                .AddSingleton(AnsiConsole.Console.Profile)

                // Transient, like the processors it aggregates: a singleton root processor would resolve the
                // transient ICommandSettingsProcessor instances once and share them between every command in the
                // host, so one command's processing state would leak into the next.
                .AddTransient<CommandArgumentsRootProcessor>()
                .AddTransient<ICommandSettingsProcessor, TokensArgumentsProcessor>()

                .AddSingleton(typeof(ICommandSettingsValidator<>), typeof(CommandSettingsValidator<>))
                .AddSingleton<IExceptionHandler, DefaultExceptionHandler>();

        // No AddConsole() here: SerilogConfigurationBundle already installs a console sink, and adding the
        // Microsoft console logger as well duplicated every log line on the terminal.
        Services.AddLogging();
    }
}
