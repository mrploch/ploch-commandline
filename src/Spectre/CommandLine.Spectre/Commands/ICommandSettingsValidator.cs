using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public interface ICommandSettingsValidator<TSettings>
    where TSettings : CommandSettings
{
    ValidationResult Validate(CommandContext context, TSettings settings);
}