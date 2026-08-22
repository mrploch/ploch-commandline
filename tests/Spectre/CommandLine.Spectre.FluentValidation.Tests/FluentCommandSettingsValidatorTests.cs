using FluentAssertions;
using FluentValidation;
using Objectivity.AutoFixture.XUnit2.AutoMoq.Attributes;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

/// <summary>
///     Cover for the FluentValidation-backed settings validator, including the fallback it takes when no
///     FluentValidation validator has been registered for the settings type.
/// </summary>
public class FluentCommandSettingsValidatorTests
{
    [Theory]
    [AutoMockData]
    public void Validate_should_fall_back_to_the_built_in_settings_validation_when_no_fluent_validator_is_supplied(CommandContext context)
    {
        var validator = new FluentCommandSettingsValidator<SelfValidatingSettings>();

        var result = validator.Validate(context, new SelfValidatingSettings { Message = "rejected by the settings themselves" });

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("rejected by the settings themselves");
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_report_success_when_the_fluent_validator_accepts_the_settings(CommandContext context)
    {
        var validator = new FluentCommandSettingsValidator<TestCommandSettings>(new AcceptingValidator());

        validator.Validate(context, new TestCommandSettings()).Successful.Should().BeTrue();
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_report_the_fluent_validator_failures(CommandContext context)
    {
        var validator = new FluentCommandSettingsValidator<TestCommandSettings>(new RejectingValidator());

        var result = validator.Validate(context, new TestCommandSettings());

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Not Empty String Property");
    }

    private sealed class SelfValidatingSettings : CommandSettings
    {
        public string Message { get; init; } = string.Empty;

        public override global::Spectre.Console.ValidationResult Validate() => global::Spectre.Console.ValidationResult.Error(Message);
    }

    private sealed class AcceptingValidator : AbstractValidator<TestCommandSettings>
    {
    }

    private sealed class RejectingValidator : AbstractValidator<TestCommandSettings>
    {
        public RejectingValidator() => RuleFor(settings => settings.NotEmptyStringProperty).NotEmpty();
    }
}
