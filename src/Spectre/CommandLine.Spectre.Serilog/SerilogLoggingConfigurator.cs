using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common;
using Serilog;
using Serilog.Events;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides extension methods for configuring Serilog logging in a command-line application.
/// </summary>
public static class SerilogLoggingConfigurator
{
    private const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    private const int RetainedFileCountLimit = 10;

    /// <summary>
    ///     Adds Serilog to the service collection with a predefined configuration.
    /// </summary>
    /// <param name="services">The service collection to add Serilog to.</param>
    /// <param name="configuration">
    ///     Optional configuration to be used for Serilog settings. If provided, will override default
    ///     settings.
    /// </param>
    /// <param name="logName">Optional name for the log file. If not provided, the current process name will be used.</param>
    /// <param name="logPath">
    ///     Optional path where log files will be stored. If not provided, the application's base directory
    ///     will be used.
    /// </param>
    /// <returns>The service collection with Serilog added for chaining additional service registrations.</returns>
    public static IServiceCollection AddSerilog(this IServiceCollection services, IConfiguration? configuration = null, string? logName = null, string? logPath = null)
    {
        return services.AddSerilog((_, loggerConfiguration) => loggerConfiguration.ConfigureSerilog(configuration, logName, logPath));
    }

    /// <summary>
    ///     Configures a Serilog logger with standard settings for command-line applications.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to modify.</param>
    /// <param name="configuration">
    ///     Optional configuration to be used for Serilog settings. If provided, will override default
    ///     settings.
    /// </param>
    /// <param name="template">A message template describing the format used to write to the sink.</param>
    /// <param name="logName">Optional name for the log file. If not provided, the current process name will be used.</param>
    /// <param name="logPath">
    ///     Optional path where log files will be stored. If not provided, the application's base directory
    ///     will be used.
    /// </param>
    /// <returns>The configured logger configuration for further customization or use.</returns>
    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration loggerConfiguration,
                                                       IConfiguration? configuration = null,
                                                       string template = DefaultOutputTemplate,
                                                       string? logName = null,
                                                       string? logPath = null)
    {
        var config = loggerConfiguration.Enrich.FromLogContext()
                                        .Enrich.WithThreadId()
                                        .Enrich.WithThreadName()
                                        .Enrich.FromLogContext()
                                        .WriteTo
                                        .File(BuildFullLogPath(logName, logPath),
                                              rollOnFileSizeLimit: true,
                                              fileSizeLimitBytes: ContentSizes.MegabytesToBytes(2),
                                              outputTemplate: template,
                                              retainedFileCountLimit: RetainedFileCountLimit,
                                              formatProvider: CultureInfo.CurrentCulture)
                                        .WriteTo
                                        .Logger(l => l.Filter.ByIncludingOnly(logEvent => logEvent.Level is LogEventLevel.Error
                                                                                              or LogEventLevel.Warning
                                                                                              or LogEventLevel.Fatal))
                                        .WriteTo.File(BuildFullLogPath(logName, logPath, "errors"),
                                                      outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {NewLine}{Exception}",
                                                      rollOnFileSizeLimit: true,
                                                      fileSizeLimitBytes: 2 * 1024 * 1024,
                                                      retainedFileCountLimit: 10,
                                                      formatProvider: CultureInfo.CurrentCulture)
                                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}",
                                                         formatProvider: CultureInfo.CurrentCulture)
                                        .MinimumLevel.Verbose();

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
