using System.Globalization;
using Ploch.Common.ArgumentChecking;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides the <see cref="AppBuilder" /> entry point for opting an application in to Serilog logging.
/// </summary>
/// <remarks>
///     <para>
///         <c>Ploch.CommandLine.Spectre</c> does not register a logging provider of its own: an application logs
///         through the providers the host installs by default. Calling <see cref="UseSerilog" /> on the builder
///         replaces them with Serilog, configured by <see cref="SerilogLoggingConfigurator.AddSerilog" /> with the
///         predefined sinks — a rolling log file, a separate file for warnings and above, and the Spectre.Console
///         console sink — and any further settings read from the <c>Serilog</c> section of the host configuration.
///     </para>
///     <para>
///         The registration is queued through <see cref="AppBuilder.ConfigureServices(Action{Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceCollection})" />,
///         so it takes its place in the builder's ordered sequence of service delegates and sees the host's
///         configuration.
///     </para>
/// </remarks>
public static class AppBuilderSerilogExtensions
{
    /// <summary>
    ///     Registers Serilog as the application's logging provider with the predefined command-line configuration.
    /// </summary>
    /// <param name="appBuilder">The builder of the application that opts in to Serilog.</param>
    /// <param name="logName">
    ///     Optional name for the log files. If not provided, the current process name is used. This affects both the
    ///     main log file and the error-specific log file names.
    /// </param>
    /// <param name="logPath">
    ///     Optional directory path where the log files are written. If not provided, the application's base directory
    ///     is used. Anchor the path to the application (for example <c>Path.Combine(AppContext.BaseDirectory, "logs")</c>);
    ///     a relative path resolves against the working directory the CLI was launched from.
    /// </param>
    /// <param name="template">An optional Serilog output template applied to the rolling log file.</param>
    /// <param name="culture">
    ///     Optional culture used to format values in both log files. When <see langword="null" />, the files are
    ///     written with <see cref="CultureInfo.InvariantCulture" />, so their contents do not depend on the locale of
    ///     the machine. Console output is not affected.
    /// </param>
    /// <returns>The same <see cref="AppBuilder" /> instance, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appBuilder" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     <para>
    ///         Call it once. Every call queues another Serilog registration, as does
    ///         <see cref="SerilogLoggingConfigurator.AddSerilog" /> on the service collection; an application that
    ///         already calls <c>services.AddSerilog(context.Configuration, ...)</c> from <c>ConfigureServices</c>
    ///         does not need this method as well; it only has to reference this package directly, which the core
    ///         package no longer does on its behalf.
    ///     </para>
    ///     <para>
    ///         Serilog reads its minimum level and overrides from the <c>Serilog</c> section of the host configuration,
    ///         that is <c>appsettings.json</c>, environment variables and the command line as
    ///         <c>Host.CreateDefaultBuilder</c> loads them.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// using Ploch.CommandLine.Spectre;
    /// using Ploch.CommandLine.Spectre.Serilog;
    ///
    /// using var appBuilder = AppBuilder.Create(args)
    ///                                  .UseSerilog(logName: "my-tool",
    ///                                              logPath: Path.Combine(AppContext.BaseDirectory, "logs"));
    ///
    /// // Register your commands with appBuilder.ConfigureCommandApp(...) and run them as usual.
    /// </code>
    /// </example>
    public static AppBuilder UseSerilog(this AppBuilder appBuilder,
                                        string? logName = null,
                                        string? logPath = null,
                                        string? template = null,
                                        CultureInfo? culture = null)
    {
        return appBuilder.NotNull()
                         .ConfigureServices((context, services) => services.AddSerilog(context.Configuration, logName, logPath, template, culture));
    }
}
