using Ploch.Common.ArgumentChecking;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Validates command settings for Spectre.Console command-line applications.
/// </summary>
/// <typeparam name="TSettings">The type of command settings to validate.</typeparam>
public class CommandSettingsValidator<TSettings> : ICommandSettingsValidator<TSettings> where TSettings : CommandSettings
{
    /// <summary>
    ///     Validates the provided command settings.
    /// </summary>
    /// <param name="context">The command context in which the validation is performed.</param>
    /// <param name="settings">The command settings to validate.</param>
    /// <returns>A <see cref="ValidationResult" /> indicating whether the validation was successful or not.</returns>
    public virtual ValidationResult Validate(CommandContext context, TSettings settings)
    {
        context.NotNull();
        settings.NotNull();

        return settings.Validate();
    }
}
