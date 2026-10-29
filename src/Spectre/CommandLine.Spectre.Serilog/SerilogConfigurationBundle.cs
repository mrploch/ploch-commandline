using System.Globalization;
using Microsoft.Extensions.Configuration;
using Ploch.Common.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Serilog;

/// <summary>
///     Provides a <see cref="ServicesBundle" /> for configuring Serilog logging, optionally reading further settings from
///     an <see cref="IConfiguration" />.
///     Allows optional customisation of the log output template, log name, log file path, and the culture used to
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
    ///     Calls <see cref="Configure" /> whether or not a configuration was supplied to the bundle.
    /// </summary>
    /// <remarks>
    ///     The base implementation refuses to run when <see cref="ServicesBundle.Configuration" /> is
    ///     <see langword="null" />. Every value this bundle reads from configuration is optional, though: without one the
    ///     logger uses the predefined sinks and enrichers and the <c>Information</c> minimum level. Dispatching to
    ///     <see cref="Configure" /> in both cases lets <c>services.AddSerilog()</c> be called without an
    ///     <see cref="IConfiguration" />, as its documentation shows, and runs a subclass's <see cref="Configure" />
    ///     override the same way with and without a configuration.
    /// </remarks>
    public override void DoConfigure() => Configure(Configuration);

    /// <summary>
    ///     Configures the services with Serilog based on the provided configuration and logging parameters.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="IConfiguration" /> that may provide additional Serilog settings, or <see langword="null" />
    ///     when the bundle was registered without one, in which case the predefined setup is used unchanged.
    /// </param>
    /// <remarks>
    ///     The parameter is declared nullable here although the base class declares it non-nullable: the base class
    ///     documents that the value can be null, and this bundle really does run without one. A subclass that overrides
    ///     this method to add its own registrations runs in both cases; declare its parameter as
    ///     <c>IConfiguration?</c> as well and tolerate <see langword="null" />.
    /// </remarks>
    protected override void Configure(IConfiguration? configuration) => RegisterSerilog(configuration);

    private void RegisterSerilog(IConfiguration? configuration)
    {
        Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration.ConfigureSerilog(configuration,
                                                                                            template,
                                                                                            logName,
                                                                                            logPath,
                                                                                            culture));
    }
}
