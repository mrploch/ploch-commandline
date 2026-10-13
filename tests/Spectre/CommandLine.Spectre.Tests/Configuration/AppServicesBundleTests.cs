using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    [Fact]
    public void Bundle_should_register_the_root_settings_processor_as_transient()
    {
        var services = BuildServices();

        var descriptor = services.Single(descriptor => descriptor.ServiceType == typeof(CommandArgumentsRootProcessor));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient, "a singleton root processor would capture the transient processors it aggregates");
    }

    [Fact]
    public void Bundle_should_register_the_token_processor_as_transient()
    {
        var services = BuildServices();

        var descriptor = services.Single(descriptor => descriptor.ServiceType == typeof(ICommandSettingsProcessor) &&
                                                       descriptor.ImplementationType == typeof(TokensArgumentsProcessor));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Bundle_should_resolve_a_fresh_root_settings_processor_for_every_resolution()
    {
        using var provider = BuildServices().BuildServiceProvider();

        var first = provider.GetRequiredService<CommandArgumentsRootProcessor>();
        var second = provider.GetRequiredService<CommandArgumentsRootProcessor>();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Bundle_should_resolve_a_fresh_token_processor_for_every_resolution()
    {
        using var provider = BuildServices().BuildServiceProvider();

        var first = provider.GetServices<ICommandSettingsProcessor>().OfType<TokensArgumentsProcessor>().Single();
        var second = provider.GetServices<ICommandSettingsProcessor>().OfType<TokensArgumentsProcessor>().Single();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void ProcessArguments_should_not_share_token_processor_state_between_two_invocations()
    {
        var processors = new List<ObservableTokensArgumentsProcessor>();
        var services = BuildServices();

        // Registered with whatever lifetime the bundle gave the real token processor, so this test fails as well
        // if that registration ever becomes a singleton again, not only the descriptor test.
        var tokenProcessorLifetime = services.Single(descriptor => descriptor.ImplementationType == typeof(TokensArgumentsProcessor)).Lifetime;
        services.Add(new ServiceDescriptor(typeof(ICommandSettingsProcessor),
                                           _ =>
                                           {
                                               var processor = new ObservableTokensArgumentsProcessor();
                                               processors.Add(processor);

                                               return processor;
                                           },
                                           tokenProcessorLifetime));
        using var provider = services.BuildServiceProvider();
        var firstSettings = new OutputSettings { OutputPath = "out/{date}" };
        var secondSettings = new ReportSettings { ReportName = "report-{date}" };

        // Two commands in one host each resolve their own pipeline and process their own settings. The base
        // processor clears its collected properties on every call, so the guard here is instance isolation: a
        // shared processor would show up as one instance, and as the second invocation's state in both slots.
        provider.GetRequiredService<CommandArgumentsRootProcessor>().ProcessArguments(firstSettings);
        provider.GetRequiredService<CommandArgumentsRootProcessor>().ProcessArguments(secondSettings);

        processors.Should().HaveCount(2).And.OnlyHaveUniqueItems("every resolution of the pipeline gets its own token processor");
        processors[0]
            .CollectedProperties.Should()
            .ContainSingle("the first invocation's state is untouched by the second one")
            .Which.Should()
            .Be(nameof(OutputSettings.OutputPath));
        processors[1].CollectedProperties.Should().ContainSingle().Which.Should().Be(nameof(ReportSettings.ReportName));
        firstSettings.OutputPath.Should().NotContain("{date}");
        secondSettings.ReportName.Should().NotContain("{date}");
    }

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
        public string? OutputPath { get; set; }
    }

    private sealed class ReportSettings : CommandSettings
    {
        [SupportsTokens]
        public string? ReportName { get; set; }
    }
}
