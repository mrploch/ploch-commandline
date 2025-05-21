using Ploch.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public class CommandSettingsValidator<TSettings> : ICommandSettingsValidator<TSettings>
    where TSettings : CommandSettings
{
    public virtual ValidationResult Validate(CommandContext context, TSettings settings)
    {
        context.NotNull();
        settings.NotNull();

        return settings.Validate();
    }
}
