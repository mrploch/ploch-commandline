using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Objectivity.AutoFixture.XUnit2.AutoMoq.Attributes;
using Ploch.CommandLine.Spectre.DependencyInjection;
using Ploch.Common.DependencyInjection;
using PlochCommandLine.Spectre.FluentValidation;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class CommandLineFluentValidationServicesBundleTests
{
    [Theory]
    [AutoMockData]
    public void Configure_adds_FluentCommandSettingsValidator_which_resolves_proper_FluentValidations_validator(CommandContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestCommand>();
        services.AddServicesBundle<AppServicesBundle>()
                .AddCommandLineSettingsFluentValidation(builder => builder.AddAssembly(typeof(TestCommandSettingsValidator).Assembly));

        var serviceProvider = services.BuildServiceProvider();

        var command = serviceProvider.GetRequiredService<TestCommand>();
        var validationResult = command.Validate(context, new TestCommandSettings());

        validationResult.Successful.Should().BeFalse();
        validationResult.Message.Should().Contain("Not Empty String Property").And.Contain("Positive Int Property");
    }
}
