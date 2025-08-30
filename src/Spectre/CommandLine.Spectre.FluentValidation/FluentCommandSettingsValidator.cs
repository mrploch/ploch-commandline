using FluentValidation;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PlochCommandLine.Spectre.FluentValidation;

/// <summary>
///     A command settings validator that uses FluentValidation to validate command settings.
/// </summary>
/// <typeparam name="TSettings">The type of command settings to validate.</typeparam>
/// <param name="fluentValidator">
///     Optional FluentValidation validator for the settings. If not provided, falls back to the
///     built-in validation.
/// </param>
public class FluentCommandSettingsValidator<TSettings>(IValidator<TSettings>? fluentValidator = null) : ICommandSettingsValidator<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    ///     Validates the command settings using FluentValidation.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings to validate.</param>
    /// <returns>
    ///     A <see cref="ValidationResult" /> indicating whether validation was successful.
    ///     If no FluentValidator was provided, falls back to the built-in validation.
    /// </returns>
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
