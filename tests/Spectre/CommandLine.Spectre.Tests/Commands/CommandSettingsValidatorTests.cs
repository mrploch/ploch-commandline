using Ploch.CommandLine.Spectre.Commands;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the default settings validator, which delegates to <see cref="CommandSettings.Validate" /> after
///     guarding its arguments.
/// </summary>
public class CommandSettingsValidatorTests
{
    [Theory]
    [AutoMockData]
    public void Validate_should_return_the_result_produced_by_the_settings(CommandContext context)
    {
        var validator = new CommandSettingsValidator<StubSettings>();

        var result = validator.Validate(context, new StubSettings { Result = ValidationResult.Error("not allowed") });

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("not allowed");
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_return_success_when_the_settings_are_valid(CommandContext context)
    {
        var validator = new CommandSettingsValidator<StubSettings>();

        validator.Validate(context, new StubSettings()).Successful.Should().BeTrue();
    }

    [Theory]
    [AutoMockData]
    public void Validate_should_reject_null_settings(CommandContext context)
    {
        var validator = new CommandSettingsValidator<StubSettings>();

        var act = () => validator.Validate(context, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_should_reject_a_null_context()
    {
        var validator = new CommandSettingsValidator<StubSettings>();

        var act = () => validator.Validate(null!, new StubSettings());

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class StubSettings : CommandSettings
    {
        public ValidationResult Result { get; init; } = ValidationResult.Success();

        public override ValidationResult Validate() => Result;
    }
}
