using Microsoft.Extensions.Configuration;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Config;

/// <summary>
///     Command to get a configuration value by key.
/// </summary>
public class ConfigGetCommand(ICommandSettingsValidator<ConfigGetCommandSettings> validator,
                              IExceptionHandler exceptionHandler,
                              IOutput output,
                              IConfiguration configuration) : AppCommand<ConfigGetCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext? context, ConfigGetCommandSettings settings, CancellationToken cancellationToken)
    {
        var value = configuration[settings.Key];

        if (value is null)
        {
            output.MarkupLineInterpolated($"[yellow]Configuration key '{settings.Key}' not found.[/]");

            return ExitCode.Error;
        }

        output.MarkupLineInterpolated($"[cyan]{settings.Key}[/]: [bold green]{value}[/]");

        return ExitCode.Success;
    }
}
