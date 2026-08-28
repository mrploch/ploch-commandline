using FluentValidation;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Users.Validators;

/// <summary>
///     FluentValidation validator for <see cref="UserAddCommandSettings" />.
///     Demonstrates automatic validation integration with Ploch.CommandLine.Spectre.FluentValidation.
/// </summary>
public class UserAddCommandSettingsValidator : AbstractValidator<UserAddCommandSettings>
{
    public UserAddCommandSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("User name is required.")
            .MinimumLength(2).WithMessage("User name must be at least 2 characters long.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid email address must be provided.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role cannot be empty.")
            .Must(role => role is "Administrator" or "Developer" or "Viewer" or "Contributor")
            .WithMessage("Role must be one of: Administrator, Developer, Viewer, Contributor.");
    }
}
