using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Command to set a configuration value.
/// </summary>
public class ConfigSetCommand(ICommandSettingsValidator<ConfigSetCommandSettings> validator,
                              IExceptionHandler exceptionHandler,
                              IOutput output) : AppCommand<ConfigSetCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext? context, ConfigSetCommandSettings settings, CancellationToken cancellationToken)
    {
        output.MarkupLineInterpolated($"[dim]Setting configuration in scope '{settings.Scope}'...[/]");
        output.MarkupLineInterpolated($"[green]Set '{settings.Key}' = '{settings.Value}' (Scope: {settings.Scope})[/]");

        return ExitCode.Success;
    }
}
