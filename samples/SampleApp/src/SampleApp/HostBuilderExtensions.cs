using Microsoft.Extensions.Hosting;

namespace Ploch.CommandLine.Spectre.SampleApp;

/// <summary>
///     Host configuration shared by the application entry point and its tests.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    ///     The name of the settings file deployed alongside the executable.
    /// </summary>
    public const string SettingsFileName = "appsettings.json";

    /// <summary>
    ///     Points the host's content root at the directory the application was deployed to, so that the host's own
    ///     <c>appsettings.json</c> and <c>appsettings.{Environment}.json</c> sources are found there rather than in the
    ///     directory the user happened to run the tool from.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="deploymentDirectory">
    ///     The directory the application was deployed to, normally <see cref="AppContext.BaseDirectory" />.
    /// </param>
    /// <returns><paramref name="hostBuilder" />, so the call chains.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostBuilder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="deploymentDirectory" /> is <see langword="null" />, empty or white space.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    ///     Thrown when <paramref name="deploymentDirectory" /> holds no <c>appsettings.json</c>. A missing configuration
    ///     file should fail loudly at start-up rather than produce a tool that behaves differently depending on the
    ///     directory it was launched from.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Moving the content root is deliberate, and so is what this method does <em>not</em> do: it does not add an
    ///         <c>appsettings.json</c> source of its own. The host has already added one, below the environment variables
    ///         and the command-line arguments. A source added afterwards is appended, which puts it on <em>top</em> of
    ///         both, so <c>--SampleAppSettings:MaxBatchSize=5</c> would be silently overridden by the file it is meant to
    ///         override. Anchoring the content root fixes the lookup path without touching the precedence order.
    ///     </para>
    ///     <para>
    ///         Anchor the content root before the host reads its configuration — from
    ///         <c>AppBuilder.ConfigureHost</c>, which runs during the host configuration phase, ahead of the application
    ///         configuration phase that loads the files.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// AppBuilder.Create(args)
    ///           .ConfigureHost(host => host.UseSettingsFromDeploymentDirectory(AppContext.BaseDirectory));
    ///     </code>
    /// </example>
    public static IHostBuilder UseSettingsFromDeploymentDirectory(this IHostBuilder hostBuilder, string deploymentDirectory)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentDirectory);

        var settingsFile = Path.Combine(deploymentDirectory, SettingsFileName);
        if (!File.Exists(settingsFile))
        {
            throw new FileNotFoundException($"The application settings file '{SettingsFileName}' was not found in the deployment directory.", settingsFile);
        }

        return hostBuilder.UseContentRoot(deploymentDirectory);
    }
}
