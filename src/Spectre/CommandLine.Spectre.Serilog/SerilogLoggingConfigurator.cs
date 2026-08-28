using Microsoft.Extensions.Configuration;
using Ploch.Common.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides extension methods for configuring Serilog logging in command-line applications
///     with integration into the Microsoft.Extensions.DependencyInjection framework.
/// </summary>
/// <remarks>
///     This static class offers convenient extension methods that simplify the process of adding
///     and configuring Serilog as the logging provider in command-line applications. The methods
///     handle both the services bundle registration and the direct Serilog configuration,
///     providing a streamlined setup process.
///     <para>
///         The configurator integrates with the services bundle pattern used throughout the
///         application, ensuring consistent dependency injection practices while providing
///         Serilog-specific configuration capabilities.
///     </para>
/// </remarks>
public static class SerilogLoggingConfigurator
{
    /// <summary>
    ///     Adds Serilog to the service collection with a comprehensive predefined configuration
    ///     optimized for command-line applications.
    /// </summary>
    /// <param name="services">
    ///     The service collection to configure. Serilog will be registered as the primary
    ///     logging provider, replacing any existing logging configuration.
    /// </param>
    /// <param name="configuration">
    ///     Optional configuration instance to read Serilog settings from. If provided, settings
    ///     from the "Serilog" section will be used to customize the logging behavior. The
    ///     configuration will be applied after the predefined setup, allowing for overrides.
    /// </param>
    /// <param name="logName">
    ///     Optional name for the log files. If not provided, the current process name will be used.
    ///     This affects both the main log file and the error-specific log file names.
    /// </param>
    /// <param name="logPath">
    ///     Optional directory path where log files will be stored. If not provided, the application's
    ///     base directory will be used. Both main and error log files will be created in this directory.
    /// </param>
    /// <param name="template">An optional Serilog output template applied to the rolling log file.</param>
    /// <returns>
    ///     The same <see cref="IServiceCollection" /> instance to enable method chaining for
    ///     additional service registrations.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method registers a <see cref="SerilogConfigurationBundle" /> with the service collection, and
    ///         nothing else. It previously also configured Serilog a second time through the
    ///         Microsoft.Extensions.Logging integration, which silently dropped the output template; that call was
    ///         removed so the bundle is the single place the logger is configured.
    ///     </para>
    ///     <para>
    ///         The bundle configures multiple sinks (file, console, Spectre.Console), enrichers (thread information,
    ///         context), and formatting options suited to command-line applications. File logging includes automatic
    ///         rotation and separate error logging.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> is <c>null</c>.
    /// </exception>
    /// <example>
    ///     <code>
    /// // Basic setup with default configuration
    /// services.AddSerilog();
    ///
    /// // With configuration and custom log settings
    /// services.AddSerilog(
    ///     configuration: configuration,
    ///     logName: "MyCommandLineApp",
    ///     logPath: @"C:\Logs"
    /// );
    ///
    /// // Method chaining with other services:
    /// services.AddSerilog(configuration)
    ///         .AddScoped&lt;IMyService, MyService&gt;()
    ///         .AddSingleton&lt;IAnotherService, AnotherService&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSerilog(this IServiceCollection services,
                                                IConfiguration? configuration = null,
                                                string? logName = null,
                                                string? logPath = null,
                                                string? template = null)
    {
        // Registered exactly once, via the bundle. Previously this also called services.AddSerilog(...)
        // directly, which configured the logger a second time and silently dropped the output template.
        return services.AddServicesBundle(new SerilogConfigurationBundle(template, logName, logPath), configuration);
    }
}
