using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Command demonstrating multiple positional arguments plus an option with a constrained set of values.
/// </summary>
/// <remarks>
///     The change is previewed, not persisted. The sample has no writable configuration store, and the
///     configuration providers the host builds (JSON file, environment variables) are read-only, so a
///     value "set" here could not be observed by <c>config get</c> in the next invocation. Rather than
///     report a change that did not happen, the command shows what it would write.
/// </remarks>
public class ConfigSetCommand(ICommandSettingsValidator<ConfigSetCommandSettings> validator,
                              IExceptionHandler exceptionHandler,
                              IOutput output) : AppCommand<ConfigSetCommandSettings>(validator, exceptionHandler)
{
    private static readonly string[] SupportedScopes = ["user", "system"];

    /// <inheritdoc />
    protected override ExitCode DoExecute(CommandContext? context, ConfigSetCommandSettings settings, CancellationToken cancellationToken)
    {
        if (!SupportedScopes.Contains(settings.Scope, StringComparer.OrdinalIgnoreCase))
        {
            output.MarkupLineInterpolated($"[red]Unsupported scope '{settings.Scope}'. Supported scopes: {string.Join(", ", SupportedScopes)}.[/]");

            return ExitCode.InvalidInput;
        }

        // The value is echoed back, so a secret passed on the command line would land in console output and any
        // CI log capturing it. `config get` and `config show` already redact on this policy; this path did not.
        // Only the rendered value is redacted - which key may be *set* is a separate question from what may be
        // disclosed, so no allow-list is applied here.
        var renderedValue = ConfigurationDisclosurePolicy.IsSensitive(settings.Key) ? "<redacted>" : settings.Value;

        output.MarkupLineInterpolated($"[green]Would set '{settings.Key}' = '{renderedValue}' in the '{settings.Scope}' scope.[/]");
        output.MarkupLineInterpolated($"[dim]Preview only - this sample has no writable configuration store, so nothing is persisted.[/]");

        return ExitCode.Success;
    }
}
