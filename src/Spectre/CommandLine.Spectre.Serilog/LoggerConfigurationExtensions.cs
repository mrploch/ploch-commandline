using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Ploch.Common;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SpectreConsole;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides extension methods for configuring Serilog logger instances with predefined settings
///     optimized for command-line applications using Spectre.Console.
/// </summary>
/// <remarks>
///     This static class contains extension methods that configure Serilog with a comprehensive
///     logging setup including multiple sinks, enrichers, and formatting options. The configuration
///     is specifically designed for command-line applications that need both file and console logging
///     with enhanced formatting capabilities provided by Spectre.Console.
/// </remarks>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    ///     The default output template used for log formatting when no custom template is provided.
    /// </summary>
    /// <remarks>
    ///     This template includes timestamp with timezone, log level, message, and exception information.
    ///     Format: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    /// </remarks>
    private const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    ///     The default number of log files to retain when file rotation occurs.
    /// </summary>
    /// <remarks>
    ///     This limit applies to both the main log files and error-specific log files to prevent
    ///     unlimited disk space usage from log file accumulation.
    /// </remarks>
    private const int RetainedFileCountLimit = 10;

    /// <summary>
    ///     Configures a Serilog logger with comprehensive settings for command-line applications.
    /// </summary>
    /// <param name="loggerConfiguration">
    ///     The Serilog logger configuration instance to configure.
    /// </param>
    /// <param name="configuration">
    ///     Optional configuration instance to read Serilog settings from. If provided, settings from
    ///     the "Serilog:MinimumLevel:Default" section will be used to determine the minimum log level.
    ///     Additional configuration will be applied after the predefined setup.
    /// </param>
    /// <param name="template">
    ///     Optional custom output template for log formatting. If not provided, the default template
    ///     will be used which includes timestamp, log level, message, and exception information.
    /// </param>
    /// <param name="logName">
    ///     Optional name for the log files. If not provided, the current process name will be used.
    ///     This affects both the main log file and the error-specific log file names.
    /// </param>
    /// <param name="logPath">
    ///     Optional directory path where log files will be stored. If not provided, the application's
    ///     base directory will be used. Both main and error log files will be created in this directory.
    /// </param>
    /// <returns>
    ///     The configured <see cref="LoggerConfiguration" /> instance for method chaining.
    /// </returns>
    /// <remarks>
    ///     This method sets up a comprehensive logging configuration that includes:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Context and thread enrichment for better log correlation</description>
    ///         </item>
    ///         <item>
    ///             <description>File logging with 2MB size limit and automatic rotation</description>
    ///         </item>
    ///         <item>
    ///             <description>Separate error log file for warnings, errors, and fatal messages</description>
    ///         </item>
    ///         <item>
    ///             <description>Console output for immediate feedback</description>
    ///         </item>
    ///         <item>
    ///             <description>Spectre.Console integration for enhanced console formatting</description>
    ///         </item>
    ///         <item>
    ///             <description>Configurable minimum log level from configuration</description>
    ///         </item>
    ///         <item>
    ///             <description>File retention policy to prevent disk space issues</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The configuration process first applies the predefined settings, then reads additional
    ///         configuration from the provided <paramref name="configuration" /> if available. This allows
    ///         for both consistent defaults and flexible customization.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Basic configuration
    /// var logger = new LoggerConfiguration()
    ///     .ConfigureSerilog()
    ///     .CreateLogger();
    ///
    /// // With custom settings
    /// var logger = new LoggerConfiguration()
    ///     .ConfigureSerilog(
    ///         configuration: config,
    ///         template: "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}",
    ///         logName: "MyApp",
    ///         logPath: @"C:\Logs"
    ///     )
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration loggerConfiguration,
                                                       IConfiguration? configuration = null,
                                                       string? template = null,
                                                       string? logName = null,
                                                       string? logPath = null)
    {
        var logMinimumLevelString =
            configuration?.GetSection("Serilog:MinimumLevel:Default").Value.SafeParseToEnum<LogEventLevel>() ?? LogEventLevel.Information;

        var config =
            loggerConfiguration.Enrich.FromLogContext()
                               .Enrich.WithThreadId()
                               .Enrich.WithThreadName()
                               .Enrich.FromLogContext()
                               .MinimumLevel.Is(logMinimumLevelString)
                               .WriteTo
                               .File(BuildFullLogPath(logName, logPath),
                                     rollOnFileSizeLimit: true,
                                     fileSizeLimitBytes: ContentSizes.MegabytesToBytes(2),
                                     outputTemplate: template ?? DefaultOutputTemplate,
                                     retainedFileCountLimit: RetainedFileCountLimit,
                                     formatProvider: CultureInfo.CurrentCulture)
                               .WriteTo
                               .Logger(l =>
                                           l.Filter.ByIncludingOnly(logEvent =>
                                                                        logEvent.Level is LogEventLevel.Error or LogEventLevel.Warning or LogEventLevel.Fatal))
                               .WriteTo.File(BuildFullLogPath(logName, logPath, "errors"),
                                             outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {NewLine}{Exception}",
                                             rollOnFileSizeLimit: true,
                                             fileSizeLimitBytes: 2 * 1024 * 1024,
                                             retainedFileCountLimit: 10,
                                             formatProvider: CultureInfo.CurrentCulture)
                               .WriteTo.Console()

                               //.WriteTo.SpectreConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}", logMinimumLevelString);
                               .WriteTo.SpectreConsole(minLevel: LogEventLevel.Verbose);

        // .MinimumLevel.Is(logMinimumLevelString);

        if (configuration != null)
        {
            config.ReadFrom.Configuration(configuration);
        }

        return config;
    }

    /// <summary>
    ///     Builds a full path for a log file based on the provided parameters.
    /// </summary>
    /// <param name="logName">The name of the log file. If null, the current process name will be used.</param>
    /// <param name="logPath">
    ///     The directory path where the log file will be stored. If null, the application's base directory
    ///     will be used.
    /// </param>
    /// <param name="suffix">Optional suffix to append to the log file name, useful for categorizing different log types.</param>
    /// <returns>A full path to the log file, combining the directory path and file name with appropriate extension.</returns>
    private static string BuildFullLogPath(string? logName, string? logPath, string? suffix = null)
    {
        logName ??= Process.GetCurrentProcess().ProcessName;
        logPath ??= AppDomain.CurrentDomain.BaseDirectory;
        suffix = suffix != null ? $"-{suffix}" : null;

        return Path.Combine(logPath, $"{logName}{suffix}.log");
    }
}
