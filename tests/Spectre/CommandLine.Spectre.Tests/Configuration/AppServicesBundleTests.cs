using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Configuration;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Ploch.Common.DependencyInjection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Configuration;

/// <summary>
///     Cover for the lifetimes <see cref="AppServicesBundle" /> registers for the settings-processing pipeline. The
///     pipeline is transient end to end: a singleton <see cref="CommandArgumentsRootProcessor" /> would resolve the
///     transient processors once and hand the same <see cref="TokensArgumentsProcessor" />, with its collected-property
///     state, to every command in the host (PLO-781).
/// </summary>
/// <remarks>
///     In the console-state collection because the bundle captures <c>AnsiConsole.Console</c> when it registers, and
///     other tests in that collection swap the console.
/// </remarks>
[Collection(GlobalConsoleState.Name)]
public sealed class AppServicesBundleTests
{
    /// <summary>
    ///     Verifies that the root processor registration cannot capture transient processors as a singleton.
    /// </summary>
    [Fact]
    public void Bundle_should_register_the_root_settings_processor_as_transient()
    {
        var services = BuildServices();

        var descriptor = services.Single(descriptor => descriptor.ServiceType == typeof(CommandArgumentsRootProcessor));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient, "a singleton root processor would capture the transient processors it aggregates");
    }

    /// <summary>
    ///     Verifies that the bundle registers the token processor with a transient lifetime.
    /// </summary>
    [Fact]
    public void Bundle_should_register_the_token_processor_as_transient()
    {
        var services = BuildServices();

        var descriptor = services.Single(descriptor => descriptor.ServiceType == typeof(ICommandSettingsProcessor) &&
                                                       descriptor.ImplementationType == typeof(TokensArgumentsProcessor));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    /// <summary>
    ///     Verifies that resolving the root settings processor twice produces separate instances.
    /// </summary>
    [Fact]
    public void Bundle_should_resolve_a_fresh_root_settings_processor_for_every_resolution()
    {
        using var provider = BuildServices().BuildServiceProvider();

        var first = provider.GetRequiredService<CommandArgumentsRootProcessor>();
        var second = provider.GetRequiredService<CommandArgumentsRootProcessor>();

        first.Should().NotBeSameAs(second);
    }

    /// <summary>
    ///     Verifies that resolving the token processor twice produces separate instances.
    /// </summary>
    [Fact]
    public void Bundle_should_resolve_a_fresh_token_processor_for_every_resolution()
    {
        using var provider = BuildServices().BuildServiceProvider();

        var first = provider.GetServices<ICommandSettingsProcessor>().OfType<TokensArgumentsProcessor>().Single();
        var second = provider.GetServices<ICommandSettingsProcessor>().OfType<TokensArgumentsProcessor>().Single();

        first.Should().NotBeSameAs(second);
    }

    /// <summary>
    ///     Verifies that two settings pipelines expand tokens while retaining separate collected-property state.
    /// </summary>
    [Fact]
    public void ProcessArguments_should_not_share_token_processor_state_between_two_invocations()
    {
        var processors = new List<ObservableTokensArgumentsProcessor>();
        var services = BuildServices();

        // The observable subclass replaces the bundle's token processor, with the lifetime the bundle gave it: the
        // substitution assertions then prove the observed instances did the work, and the test fails as well if that
        // registration ever becomes a singleton again, not only the descriptor test.
        var tokenProcessorDescriptor = services.Single(descriptor => descriptor.ImplementationType == typeof(TokensArgumentsProcessor));
        services.Remove(tokenProcessorDescriptor);
        services.Add(new ServiceDescriptor(typeof(ICommandSettingsProcessor),
                                           _ =>
                                           {
                                               var processor = new ObservableTokensArgumentsProcessor();
                                               processors.Add(processor);

                                               return processor;
                                           },
                                           tokenProcessorDescriptor.Lifetime));
        using var provider = services.BuildServiceProvider();
        var firstSettings = new OutputSettings { OutputPath = "out/{date}" };
        var secondSettings = new ReportSettings { ReportName = "report-{date}" };
        var dateBefore = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Two commands in one host each resolve their own pipeline and process their own settings. The base
        // processor clears its collected properties on every call, so the guard here is instance isolation: a
        // shared processor would show up as one instance, and as the second invocation's state in both slots.
        provider.GetRequiredService<CommandArgumentsRootProcessor>().ProcessArguments(firstSettings);
        provider.GetRequiredService<CommandArgumentsRootProcessor>().ProcessArguments(secondSettings);
        var dateAfter = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        processors.Should().HaveCount(2).And.OnlyHaveUniqueItems("every resolution of the pipeline gets its own token processor");
        processors[0]
            .CollectedProperties.Should()
            .ContainSingle("the first invocation's state is untouched by the second one")
            .Which.Should()
            .Be(nameof(OutputSettings.OutputPath));
        processors[1].CollectedProperties.Should().ContainSingle().Which.Should().Be(nameof(ReportSettings.ReportName));

        // Either date is accepted so that a UTC midnight between the two reads does not fail correct code.
        firstSettings.OutputPath.Should()
                     .BeOneOf([ $"out/{dateBefore}", $"out/{dateAfter}" ],
                              "the observed processor is the only token processor in the pipeline, and it expands {{date}} to the UTC date");
        secondSettings.ReportName.Should().BeOneOf($"report-{dateBefore}", $"report-{dateAfter}");
    }

    /// <summary>
    ///     Verifies that the bundle registers no Serilog service: Serilog is opt-in through its own package (PLO-778).
    /// </summary>
    [Fact]
    public void Bundle_should_not_register_any_serilog_service()
    {
        var services = BuildServices();

        services.Should().NotContain(descriptor => IsSerilogDescriptor(descriptor), "the core bundle leaves the choice of logging provider to the application");
    }

    /// <summary>
    ///     Verifies that the bundle keeps <see cref="ILogger{TCategoryName}" /> resolvable on its own, without a host.
    /// </summary>
    [Fact]
    public void Bundle_should_resolve_a_logger_without_a_host_or_a_provider()
    {
        using var provider = BuildServices().BuildServiceProvider();

        var logger = provider.GetRequiredService<ILogger<AppServicesBundleTests>>();

        logger.Should().NotBeNull("AddLogging stays in the bundle so the logging infrastructure is present outside a host as well");
    }

    /// <summary>
    ///     Factory- and instance-backed descriptors carry a Microsoft.Extensions.Logging service type, so the
    ///     implementation side is checked as well as the service type.
    /// </summary>
    private static bool IsSerilogDescriptor(ServiceDescriptor descriptor) =>
        IsSerilogType(descriptor.ServiceType) ||
        IsSerilogType(descriptor.ImplementationType) ||
        IsSerilogType(descriptor.ImplementationInstance?.GetType()) ||
        IsSerilogType(descriptor.ImplementationFactory?.Method.ReturnType);

    private static bool IsSerilogType(Type? type) =>
        type?.Namespace is { } typeNamespace &&
        (typeNamespace.Equals("Serilog", StringComparison.Ordinal) || typeNamespace.StartsWith("Serilog.", StringComparison.Ordinal));

    /// <summary>
    ///     Creates a service collection configured with the application bundle and an empty configuration.
    /// </summary>
    /// <returns>The service registrations used to inspect and resolve the settings pipeline.</returns>
    private static IServiceCollection BuildServices()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddServicesBundle<AppServicesBundle>(new ConfigurationBuilder().Build());

        return services;
    }

    /// <summary>
    ///     A token processor that exposes the properties it collected, which is the per-invocation state a shared
    ///     instance would leak.
    /// </summary>
    private sealed class ObservableTokensArgumentsProcessor : TokensArgumentsProcessor
    {
        public IReadOnlyCollection<string> CollectedProperties => Properties.Keys.ToArray();
    }

    private sealed class OutputSettings : CommandSettings
    {
        [SupportsTokens]
        public string? OutputPath { get; init; }
    }

    private sealed class ReportSettings : CommandSettings
    {
        [SupportsTokens]
        public string? ReportName { get; init; }
    }
}
