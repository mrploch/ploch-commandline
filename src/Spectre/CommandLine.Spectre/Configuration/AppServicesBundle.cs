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
    /// <param name="configuration">
    ///     Optional configuration to be used during service registration.
    ///     If provided, it will be passed to dependent service bundles.
    /// </param>
    /// <remarks>
    ///     This method registers:
    ///     - Spectre.Console components (Console, Input, Cursor, ExclusivityMode, Profile)
    ///     - Serilog configuration through SerilogConfigurationBundle
    ///     - Command settings validation services
    ///     - Exception handling services
    ///     - Console logging.
    /// </remarks>
    protected override void Configure(IConfiguration configuration)
    {
        // The console singleton comes from OutputServicesBundle, which is a declared dependency of this
        // bundle. Registering it here as well produced a duplicate singleton descriptor.
        Services.AddSingleton(AnsiConsole.Console.Input)
                .AddSingleton(AnsiConsole.Console.Cursor)
                .AddSingleton(AnsiConsole.Console.ExclusivityMode)
                .AddSingleton(AnsiConsole.Console.Profile)
                .AddSingleton<CommandArgumentsRootProcessor>()
                .AddTransient<ICommandSettingsProcessor, TokensArgumentsProcessor>()

                .AddSingleton(typeof(ICommandSettingsValidator<>), typeof(CommandSettingsValidator<>))
                .AddSingleton<IExceptionHandler, DefaultExceptionHandler>();

        // No AddConsole() here: SerilogConfigurationBundle already installs a console sink, and adding the
        // Microsoft console logger as well duplicated every log line on the terminal.
        Services.AddLogging();
    }
}
