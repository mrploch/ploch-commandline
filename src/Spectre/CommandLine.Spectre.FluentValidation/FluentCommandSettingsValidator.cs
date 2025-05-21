using FluentValidation;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.Tools.SystemProfiles.UI.ConsoleUI.WeatherForecasts;

public class FluentCommandSettingsValidator<TSettings>(IValidator<TSettings>? fluentValidator = null) : ICommandSettingsValidator<TSettings>
    where TSettings : CommandSettings
{
    public ValidationResult Validate(CommandContext context, TSettings settings)
    {
        if (fluentValidator == null)
        {
            return settings.Validate();
        }

        var validationResult = fluentValidator.Validate(settings);

        return validationResult.IsValid ? ValidationResult.Success() : ValidationResult.Error(validationResult.ToString());
    }
}
