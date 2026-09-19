using System.Globalization;
using Microsoft.Extensions.Configuration;
using Ploch.Common.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides a <see cref="ServicesBundle" /> for configuring Serilog logging using the specified <see cref="IConfiguration" />.
///     Allows optional customization of the log output template, log name, log file path, and the culture used to
///     format values in the log files.
/// </summary>
/// <param name="template">Optional log output template.</param>
/// <param name="logName">Optional log name.</param>
/// <param name="logPath">Optional log file path.</param>
/// <param name="culture">
///     Optional culture used to format values in both log files. When <see langword="null" />, the files are
///     written with <see cref="CultureInfo.InvariantCulture" />.
/// </param>
public class SerilogConfigurationBundle(string? template = null,
                                        string? logName = null,
                                        string? logPath = null,
                                        CultureInfo? culture = null) : ConfigurableServicesBundle
{
    /// <summary>
    ///     Configures the services with Serilog based on the provided configuration and logging parameters.
    /// </summary>
    /// <param name="configuration">
    ///     An optional instance of <see cref="IConfiguration" /> that may provide additional configuration details.
    /// </param>
    protected override void Configure(IConfiguration configuration)
    {
        Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration.ConfigureSerilog(configuration,
                                                                                            template,
                                                                                            logName,
                                                                                            logPath,
                                                                                            culture));
    }
}
