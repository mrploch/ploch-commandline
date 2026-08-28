using Microsoft.Extensions.Configuration;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Command to get a configuration value by key.
/// </summary>
/// <remarks>
///     The key is supplied by the user and the host adds an environment-variable configuration provider, so an
///     unguarded lookup would read any environment variable of the process and print it verbatim - which is what
///     this command used to do. It applies the same policy as <see cref="ConfigShowCommand" />: the key has to sit
///     inside a section this application owns, and a value whose path looks like a secret is redacted.
/// </remarks>
public class ConfigGetCommand(ICommandSettingsValidator<ConfigGetCommandSettings> validator,
                              IExceptionHandler exceptionHandler,
                              IOutput output,
                              IConfiguration configuration) : AppCommand<ConfigGetCommandSettings>(validator, exceptionHandler)
{
    /// <inheritdoc />
    protected override ExitCode DoExecute(CommandContext? context, ConfigGetCommandSettings settings, CancellationToken cancellationToken)
    {
        // Checked before the lookup: refusing after reading would still have pulled the secret into memory, and the
        // "not found" branch below would otherwise confirm whether an arbitrary environment variable exists.
        if (!ConfigurationDisclosurePolicy.IsRenderable(settings.Key))
        {
            output.MarkupLineInterpolated($"[yellow]Configuration key '{settings.Key}' is outside this application's own settings.[/]");
            output.MarkupLineInterpolated($"[dim]Readable sections: {string.Join(", ", ConfigurationDisclosurePolicy.ApplicationSections)}[/]");

            return ExitCode.InvalidInput;
        }

        var value = configuration[settings.Key];

        if (value is null)
        {
            output.MarkupLineInterpolated($"[yellow]Configuration key '{settings.Key}' not found.[/]");

            return ExitCode.Error;
        }

        if (ConfigurationDisclosurePolicy.IsSensitive(settings.Key))
        {
            output.MarkupLineInterpolated($"[cyan]{settings.Key}[/]: [yellow]<redacted>[/]");

            return ExitCode.Success;
        }

        output.MarkupLineInterpolated($"[cyan]{settings.Key}[/]: [bold green]{value}[/]");

        return ExitCode.Success;
    }
}
