using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Objectivity.AutoFixture.XUnit2.AutoMoq.Attributes;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Configuration;
using Ploch.CommandLine.Spectre.DependencyInjection;
using Ploch.Common.DependencyInjection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class CommandLineFluentValidationServicesBundleTests
{
    [Theory]
    [AutoMockData]
    public void Configure_adds_FluentCommandSettingsValidator_which_resolves_proper_FluentValidations_validator(CommandContext context)
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<TestCommand>();
        services.AddServicesBundle<AppServicesBundle>(configuration)
                .AddCommandLineSettingsFluentValidation(builder => builder.AddAssembly(typeof(TestCommandSettingsValidator).Assembly));

        var serviceProvider = services.BuildServiceProvider();

        var command = serviceProvider.GetRequiredService<TestCommand>();
        var validationResult = command.Validate(context, new TestCommandSettings());

        validationResult.Successful.Should().BeFalse();
        validationResult.Message.Should().Contain("Not Empty String Property").And.Contain("Positive Int Property");
    }

    /// <summary>
    ///     AddCommandLineSettingsFluentValidation registered the open-generic mapping itself and then invoked the
    ///     bundle, which registers the same mapping. AddSingleton appends unconditionally rather than replacing, so
    ///     IEnumerable&lt;ICommandSettingsValidator&lt;T&gt;&gt; resolved two identical instances.
    /// </summary>
    [Fact]
    public void AddCommandLineSettingsFluentValidation_should_register_the_validator_mapping_exactly_once()
    {
        var services = new ServiceCollection();

        services.AddCommandLineSettingsFluentValidation(builder => builder.AddAssembly(typeof(TestCommandSettingsValidator).Assembly));

        services.Count(descriptor => descriptor.ServiceType == typeof(ICommandSettingsValidator<>))
                .Should()
                .Be(1, "a duplicate descriptor makes IEnumerable<ICommandSettingsValidator<T>> resolve the same validator twice");
    }

    /// <summary>
    ///     FluentCommandSettingsValidator&lt;&gt; is a singleton that injects IValidator&lt;T&gt;.
    ///     AddValidatorsFromAssemblies defaults to Scoped, which made the wrapper a captive dependency: resolving it
    ///     from the root provider throws once scope validation is enabled, as it is by default in Development.
    /// </summary>
    [Fact]
    public void AddCommandLineSettingsFluentValidation_should_survive_scope_validation()
    {
        var services = new ServiceCollection();
        services.AddCommandLineSettingsFluentValidation(builder => builder.AddAssembly(typeof(TestCommandSettingsValidator).Assembly));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        var act = () => provider.GetRequiredService<ICommandSettingsValidator<TestCommandSettings>>();

        act.Should().NotThrow("a singleton wrapper must not capture a scoped validator");
    }

    /// <summary>
    ///     The case that actually matters, and the one a dependency-free validator cannot exercise: a consumer
    ///     validator that injects a scoped service, such as a DbContext or a per-request user context. The wrapper is
    ///     a singleton because Spectre resolves commands from the root provider, so it must resolve the validator
    ///     inside a scope rather than capture it.
    /// </summary>
    [Fact]
    public void Validate_should_support_a_consumer_validator_with_a_scoped_dependency()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedProbe>();
        services.AddScoped<IValidator<ScopedDependencySettings>, ScopedDependencyValidator>();
        services.AddSingleton(typeof(ICommandSettingsValidator<>), typeof(FluentCommandSettingsValidator<>));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var validator = provider.GetRequiredService<ICommandSettingsValidator<ScopedDependencySettings>>();

        var act = () => validator.Validate(null!, new ScopedDependencySettings());

        act.Should().NotThrow("the validator is resolved inside a scope, so its scoped dependency is legal");
    }

    /// <summary>
    ///     Each validation must get its own scope. If the wrapper cached the validator, or reused one scope, both
    ///     calls would observe the same scoped instance.
    /// </summary>
    [Fact]
    public void Validate_should_create_a_fresh_scope_for_every_validation()
    {
        ScopedProbe.Seen.Clear();
        var services = new ServiceCollection();
        services.AddScoped<ScopedProbe>();
        services.AddScoped<IValidator<ScopedDependencySettings>, ScopedDependencyValidator>();
        services.AddSingleton(typeof(ICommandSettingsValidator<>), typeof(FluentCommandSettingsValidator<>));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var validator = provider.GetRequiredService<ICommandSettingsValidator<ScopedDependencySettings>>();

        validator.Validate(null!, new ScopedDependencySettings());
        validator.Validate(null!, new ScopedDependencySettings());

        ScopedProbe.Seen.Should().HaveCount(2, "each validation runs in its own scope");
        ScopedProbe.Seen.Distinct().Should().HaveCount(2, "two scopes must yield two distinct scoped instances");
    }
}

/// <summary>A scoped service a consumer validator might legitimately depend on, such as a DbContext.</summary>
internal sealed class ScopedProbe
{
    public static List<Guid> Seen { get; } = [];

    public Guid Id { get; } = Guid.NewGuid();
}

internal sealed class ScopedDependencySettings : CommandSettings
{
}

/// <summary>
///     A consumer-style validator with a scoped dependency. Constructed by the container only, which is the whole
///     point: it cannot be resolved at all if the wrapper captures rather than scopes.
/// </summary>
internal sealed class ScopedDependencyValidator : AbstractValidator<ScopedDependencySettings>
{
    public ScopedDependencyValidator(ScopedProbe probe)
    {
        ScopedProbe.Seen.Add(probe.Id);
    }
}
