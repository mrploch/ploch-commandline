using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Ploch.Common.CommandLine.Serilog;

/// <summary>
///     Provides setup methods for configuring logging using Serilog in an application.
/// </summary>
public static class LoggingSetup
{
    /// <summary>
    ///     Configures the application to use Serilog for logging.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    /// <param name="logName">Optional name for the log.</param>
    /// <param name="logPath">Optional path for the log file.</param>
    /// <returns>The instance of <see cref="AppBuilder" />, allowing for method chaining.</returns>
    public static AppBuilder UseSerilog(this AppBuilder appBuilder, string? logName = null, string? logPath = null)
    {
        appBuilder.Configure(container => ConfigureServices(container.Services, logName, logPath));

        return appBuilder;
    }

    private static void ConfigureServices(IServiceCollection serviceCollection, string? logName = null, string? logPath = null)
    {
        var loggerConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                                                           .Enrich.WithThreadId()
                                                           .Enrich.WithThreadName()
                                                           .Enrich.FromLogContext()
                                                           .WriteTo.File(BuildFullLogPath(logName, logPath),
                                                                         rollOnFileSizeLimit: true,
                                                                         fileSizeLimitBytes: 2 * 1024 * 1024,
                                                                         //  outputTemplate: template,
                                                                         retainedFileCountLimit: 10,
                                                                         formatProvider: CultureInfo.CurrentCulture)
                                                           .WriteTo.Logger(l => l.Filter.ByIncludingOnly(logEvent =>
                                                                                                             logEvent.Level is LogEventLevel.Error
                                                                                                                            or LogEventLevel.Warning
                                                                                                                            or LogEventLevel.Fatal))
                                                           .WriteTo.File(BuildFullLogPath(logName, logPath, "errors"),
                                                                         rollOnFileSizeLimit: true,
                                                                         fileSizeLimitBytes: 2 * 1024 * 1024,
                                                                         //  outputTemplate: template,
                                                                         retainedFileCountLimit: 10,
                                                                         formatProvider: CultureInfo.CurrentCulture)
                                                           .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture).MinimumLevel.Error();

        serviceCollection.AddLogging(builder => builder.AddSerilog(loggerConfiguration.CreateLogger()));
    }

    private static string BuildFullLogPath(string? logName, string? logPath, string? suffix = null)
    {
        logName = logName ?? Process.GetCurrentProcess().ProcessName;
        logPath = logPath ?? AppDomain.CurrentDomain.BaseDirectory;
        suffix = suffix != null ? $"-{suffix}" : null;

        return Path.Combine(logPath, $"{logName}{suffix}.log");
    }
}