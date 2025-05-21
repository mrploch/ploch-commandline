using FluentValidation;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestCommandSettingsValidator : AbstractValidator<TestCommandSettings>
{
    public TestCommandSettingsValidator()
    {
        RuleFor(x => x.NotEmptyStringProperty).NotEmpty();
        RuleFor(x => x.PositiveIntProperty).GreaterThan(0);
    }
}
