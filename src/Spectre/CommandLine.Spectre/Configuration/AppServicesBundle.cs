using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Configuration;

/// <summary>
///     A services bundle that configures essential services for a Spectre.Console-based command-line application.
/// </summary>
/// <remarks>
///     This bundle registers Spectre.Console components, the logging services and command-related handlers
///     to provide a complete foundation for command-line applications. It does not choose a logging provider:
///     an application built through <see cref="AppBuilder" /> logs through the providers
///     <c>Host.CreateDefaultBuilder</c> installs (console, debug and event source), and Serilog is opt-in
///     through the <c>Ploch.CommandLine.Spectre.Serilog</c> package.
/// </remarks>
public class AppServicesBundle : ConfigurableServicesBundle
{
    /// <summary>
    ///     Gets the bundles registered before this one — the output services.
    /// </summary>
    protected override IEnumerable<IServicesBundle>? Dependencies => [ new OutputServicesBundle() ];

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
    ///     - The logging services, so that <c>ILogger&lt;T&gt;</c> resolves even when the bundle is used outside a host.
    ///       No provider is added here: the host supplies its defaults, and Serilog is registered only by the
    ///       <c>Ploch.CommandLine.Spectre.Serilog</c> package when the application opts in.
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

        // Only the logging infrastructure, no provider. Inside AppBuilder the host has already added its default
        // providers and this call is a no-op; outside a host it makes ILogger<T> resolvable. A provider chosen here
        // would be imposed on every consumer, which is what registering Serilog from this bundle used to do (PLO-778).
        Services.AddLogging();
    }
}
