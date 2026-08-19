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
        Services.AddSingleton(AnsiConsole.Console)
                .AddSingleton(AnsiConsole.Console.Input)
                .AddSingleton(AnsiConsole.Console.Cursor)
                .AddSingleton(AnsiConsole.Console.ExclusivityMode)
                .AddSingleton(AnsiConsole.Console.Profile)
                .AddSingleton<CommandArgumentsRootProcessor>()
                .AddKeyedTransient<ICommandSettingsProcessor, TokensArgumentsProcessor>(nameof(TokensArgumentsProcessor))
                .AddTransient<ICommandSettingsProcessor, TokensArgumentsProcessor>()

                // .AddServicesBundle(new SerilogConfigurationBundle(), configuration)
                .AddSingleton(typeof(ICommandSettingsValidator<>), typeof(CommandSettingsValidator<>))
                .AddSingleton<IExceptionHandler, DefaultExceptionHandler>();

        Services.AddLogging(builder => builder.AddConsole());
    }
}
