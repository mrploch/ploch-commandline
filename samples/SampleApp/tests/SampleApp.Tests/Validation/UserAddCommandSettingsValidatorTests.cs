using FluentAssertions;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users.Validators;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Validation;

public class UserAddCommandSettingsValidatorTests
{
    private readonly UserAddCommandSettingsValidator _validator = new();

    [Fact]
    public void Validate_should_succeed_when_settings_are_valid()
    {
        var settings = new UserAddCommandSettings
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Role = "Developer"
        };

        var result = _validator.Validate(settings);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Validate_should_fail_when_name_is_invalid(string name)
    {
        var settings = new UserAddCommandSettings
        {
            Name = name,
            Email = "john@example.com",
            Role = "Developer"
        };

        var result = _validator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UserAddCommandSettings.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@missing-user.com")]
    public void Validate_should_fail_when_email_is_invalid(string email)
    {
        var settings = new UserAddCommandSettings
        {
            Name = "Valid Name",
            Email = email,
            Role = "Developer"
        };

        var result = _validator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UserAddCommandSettings.Email));
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
    [InlineData("Invalid")]
    public void Validate_should_fail_when_role_is_not_allowed(string role)
    {
        var settings = new UserAddCommandSettings
        {
            Name = "Valid Name",
            Email = "valid@example.com",
            Role = role
        };

        var result = _validator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UserAddCommandSettings.Role));
    }
}
