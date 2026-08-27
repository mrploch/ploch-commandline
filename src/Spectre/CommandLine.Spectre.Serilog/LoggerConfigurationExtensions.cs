using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Ploch.Common;
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
    ///     Format: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".
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

    private const string ErrorOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {NewLine}{Exception}";

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
    ///         logName: "Ploch.MyApp",
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

            // ReSharper disable once ComplexConditionExpression
            loggerConfiguration.Enrich.FromLogContext()
                               .Enrich.WithThreadId()
                               .Enrich.WithThreadName()
                               .MinimumLevel.Is(logMinimumLevelString)
                               .WriteTo
                               .File(BuildFullLogPath(logName, logPath),
                                     rollOnFileSizeLimit: true,
                                     fileSizeLimitBytes: ContentSizes.MegabytesToBytes(2),
                                     outputTemplate: template ?? DefaultOutputTemplate,
                                     retainedFileCountLimit: RetainedFileCountLimit,
                                     formatProvider: CultureInfo.CurrentCulture)

                                // The error file sink must live INSIDE the filtered sub-logger. Chained after it,
                                // as it previously was, the filter applies to nothing and the "errors" file
                                // receives every event.
                               .WriteTo.Logger(errorLog => ConfigureErrorFileSink(errorLog, logName, logPath))

                                // Only the Spectre sink: adding WriteTo.Console as well duplicated every
                                // console log line.
                               .WriteTo.SpectreConsole(minLevel: LogEventLevel.Verbose);

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
        var processName = Process.GetCurrentProcess().ProcessName;

        logPath ??= AppDomain.CurrentDomain.BaseDirectory;
        suffix = suffix != null ? $"-{suffix}" : null;

        // logName names a file, not a path, and it is a public parameter of AddSerilog - so it decides where the
        // sink writes unless this method says otherwise. Two separate ways it could escape logPath:
        //
        //   rooted    logName: "C:\app"      Path.Combine discards logPath entirely and writes C:\app.log
        //   traversal logName: "../outside"  survives Path.Join as logs/../outside.log, which the OS resolves
        //                                    to a sibling of logPath
        //
        // Path.Join alone closes only the first. Stripping the directory portion closes both, and leaves an
        // ordinary name untouched. A name that is nothing but a directory part ("sub/") leaves nothing behind,
        // so the process name stands in rather than producing a file called ".log".
        var fileName = Path.GetFileName(logName ?? processName);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = processName;
        }

        return Path.Join(logPath, $"{fileName}{suffix}.log");
    }

    /// <summary>
    ///     Configures the dedicated error log: a sub-logger that admits only Warning, Error and Fatal events
    ///     and writes them to a separate rolling file.
    /// </summary>
    /// <param name="loggerConfiguration">The sub-logger configuration to populate.</param>
    /// <param name="logName">The base name of the log file.</param>
    /// <param name="logPath">The directory the log file is written to.</param>
    private static void ConfigureErrorFileSink(LoggerConfiguration loggerConfiguration, string? logName, string? logPath)
    {
        loggerConfiguration.Filter
                           .ByIncludingOnly(logEvent => logEvent.Level is LogEventLevel.Error or LogEventLevel.Warning or LogEventLevel.Fatal)
                           .WriteTo.File(BuildFullLogPath(logName, logPath, "errors"),
                                         outputTemplate: ErrorOutputTemplate,
                                         rollOnFileSizeLimit: true,
                                         fileSizeLimitBytes: ContentSizes.MegabytesToBytes(2),
                                         retainedFileCountLimit: RetainedFileCountLimit,
                                         formatProvider: CultureInfo.CurrentCulture);
    }
}
