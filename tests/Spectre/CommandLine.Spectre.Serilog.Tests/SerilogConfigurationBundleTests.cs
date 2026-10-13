using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.DependencyInjection;
using ILogger = Serilog.ILogger;

namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Cover for the bundle's configuration dispatch. The bundle must run its <c>Configure</c> hook — and therefore
///     a subclass's override of it — whether or not it was registered with an <see cref="IConfiguration" />, because
///     the base class only refuses a missing configuration and Serilog has complete defaults without one.
/// </summary>
public sealed class SerilogConfigurationBundleTests
{
    [Fact]
    public void DoConfigure_should_run_a_subclass_override_of_configure_when_a_configuration_is_supplied()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var bundle = new CustomisingBundle();

        services.AddServicesBundle(bundle, configuration);

        bundle.ConfigureCalls.Should().Be(1, "the override must run exactly once");
        bundle.ReceivedConfiguration.Should().BeSameAs(configuration, "the override receives the configuration the bundle was registered with");
        RegisteredMarker(services).Name.Should().Be("customised", "the override's own registration must be applied");
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ILogger), "the base registration must still be applied exactly once");
    }

    [Fact]
    public void DoConfigure_should_run_a_subclass_override_of_configure_when_no_configuration_is_supplied()
    {
        var services = new ServiceCollection();
        var bundle = new CustomisingBundle();

        services.AddServicesBundle(bundle);

        bundle.ConfigureCalls.Should().Be(1, "the override must run without a configuration exactly as it does with one");
        bundle.ReceivedConfiguration.Should().BeNull("there is no configuration to hand over");
        RegisteredMarker(services).Name.Should().Be("customised", "the override's own registration must be applied");
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ILogger), "the base registration must still be applied exactly once");
    }

    private static Marker RegisteredMarker(IServiceCollection services) =>
        services.Should()
                .ContainSingle(descriptor => descriptor.ServiceType == typeof(Marker))
                .Which.ImplementationInstance.Should()
                .BeOfType<Marker>()
                .Subject;

    private sealed class Marker
    {
        public string Name { get; init; } = "customised";
    }

    /// <summary>A consumer's bundle that adds a registration of its own on top of the Serilog defaults.</summary>
    private sealed class CustomisingBundle : SerilogConfigurationBundle
    {
        public int ConfigureCalls { get; private set; }

        public IConfiguration? ReceivedConfiguration { get; private set; }

        protected override void Configure(IConfiguration? configuration)
        {
            ConfigureCalls++;
            ReceivedConfiguration = configuration;
            base.Configure(configuration);
            Services.AddSingleton(new Marker());
        }
    }
}
