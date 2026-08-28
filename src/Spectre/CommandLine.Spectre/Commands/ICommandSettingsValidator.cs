using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Defines a validator for command settings.
/// </summary>
/// <typeparam name="TSettings">The type of command settings to validate.</typeparam>
public interface ICommandSettingsValidator<in TSettings> where TSettings : CommandSettings
{
    /// <summary>
    ///     Validates the command settings.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The command settings to validate.</param>
    /// <returns>
    ///     A <see cref="ValidationResult" /> indicating whether the validation succeeded or failed.
    ///     If validation fails, the result contains error information.
    /// </returns>
    ValidationResult Validate(CommandContext context, TSettings settings);
}
