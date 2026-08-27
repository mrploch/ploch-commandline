namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Decides which configuration a command may render, and which values must be redacted first.
/// </summary>
/// <remarks>
///     Shared by every command that prints configuration. The host adds an environment-variable provider, so the
///     configuration root carries every environment variable of the process — API keys and access tokens included.
///     Each command that renders configuration therefore needs both an allow-list of the sections this application
///     owns and a redaction pass over the leaves inside them.
///     It lives here rather than on one command because it was previously duplicated: <c>ConfigShowCommand</c>
///     applied both rules while <c>ConfigGetCommand</c> applied neither, so <c>config get</c> would happily print any
///     environment variable asked for by name. One policy means the two cannot drift apart again.
/// </remarks>
public static class ConfigurationDisclosurePolicy
{
    /// <summary>
    ///     The configuration sections this application owns and is willing to render.
    /// </summary>
    public static readonly string[] ApplicationSections = ["SampleAppSettings", "Logging", "Serilog"];

    private static readonly string[] SensitivePathFragments =
        ["password", "pwd", "secret", "token", "apikey", "api_key", "credential", "connectionstring", "privatekey", "accesskey"];

    /// <summary>
    ///     Gets a value indicating whether the given configuration path may be rendered at all.
    /// </summary>
    /// <param name="path">The full configuration path, for example <c>SampleAppSettings:Environment</c>.</param>
    /// <returns><see langword="true" /> when the path sits inside a section this application owns.</returns>
    /// <remarks>
    ///     The section name has to be followed by a separator or end the path, so that a key such as
    ///     <c>LoggingSecrets:ApiKey</c> is not admitted by the <c>Logging</c> entry.
    /// </remarks>
    public static bool IsRenderable(string path) =>
        ApplicationSections.Any(section => path.Equals(section, StringComparison.OrdinalIgnoreCase)
                                           || path.StartsWith(section + ":", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Gets a value indicating whether the value at the given path must be redacted before rendering.
    /// </summary>
    /// <param name="path">The full configuration path.</param>
    /// <returns><see langword="true" /> when the path looks like it names a secret.</returns>
    /// <remarks>
    ///     Matching on the full path rather than the leaf catches <c>ConnectionStrings:Default</c> as well as
    ///     <c>Args:apiKey</c>. Name matching is a heuristic, not a guarantee: it is the right shape for a sample, but
    ///     a real application should keep secrets out of renderable configuration in the first place.
    /// </remarks>
    public static bool IsSensitive(string path) =>
        SensitivePathFragments.Any(fragment => path.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
