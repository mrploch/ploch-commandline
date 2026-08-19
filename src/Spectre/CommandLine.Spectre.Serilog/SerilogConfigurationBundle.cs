using Microsoft.Extensions.Configuration;
using Ploch.Common.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides a <see cref="ServicesBundle" /> for configuring Serilog logging using the specified <see cref="IConfiguration" />.
///     Allows optional customization of the log output template, log name, and log file path.
/// </summary>
/// <param name="template">Optional log output template.</param>
/// <param name="logName">Optional log name.</param>
/// <param name="logPath">Optional log file path.</param>
public class SerilogConfigurationBundle(string? template = null, string? logName = null, string? logPath = null) : ConfigurableServicesBundle
{
    /// <summary>
    ///     Configures the services with Serilog based on the provided configuration and logging parameters.
    /// </summary>
    /// <param name="configuration">
    ///     An optional instance of <see cref="IConfiguration" /> that may provide additional configuration details.
    /// </param>
    protected override void Configure(IConfiguration configuration)
    {
        Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration.ConfigureSerilog(configuration, template, logName, logPath));
    }
}
