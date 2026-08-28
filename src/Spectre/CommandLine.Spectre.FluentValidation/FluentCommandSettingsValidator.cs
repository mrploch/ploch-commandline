using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation;

/// <summary>
///     A command settings validator that uses FluentValidation to validate command settings.
/// </summary>
/// <typeparam name="TSettings">The type of command settings to validate.</typeparam>
/// <param name="scopeFactory">
///     Factory used to create a scope per validation, from which the FluentValidation validator is resolved.
/// </param>
/// <remarks>
///     The validator is resolved per call rather than injected. This type is registered as a singleton because
///     Spectre resolves commands from the root provider, so injecting IValidator directly would capture it:
///     FluentValidation registers validators as scoped by default, and a validator may legitimately depend on a
///     DbContext, a repository, or a scoped user context. Resolving inside a scope keeps the wrapper a singleton
///     while letting consumer validators keep any lifetime and any dependencies.
/// </remarks>
public class FluentCommandSettingsValidator<TSettings>(IServiceScopeFactory scopeFactory) : ICommandSettingsValidator<TSettings>
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
        using var scope = scopeFactory.CreateScope();
        var fluentValidator = scope.ServiceProvider.GetService<IValidator<TSettings>>();

        if (fluentValidator == null)
        {
            return settings.Validate();
        }

        var validationResult = fluentValidator.Validate(settings);

        return validationResult.IsValid ? ValidationResult.Success() : ValidationResult.Error(validationResult.ToString());
    }
}
