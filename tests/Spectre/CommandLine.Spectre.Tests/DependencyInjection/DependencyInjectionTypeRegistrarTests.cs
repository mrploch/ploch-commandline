using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Spectre.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Tests.DependencyInjection;

/// <summary>
///     Cover for the bridge between Spectre.Console.Cli's type registry and Microsoft.Extensions.DependencyInjection.
///     Every registration made here has to be visible from the resolver the registrar builds.
/// </summary>
public class DependencyInjectionTypeRegistrarTests
{
    [Fact]
    public void Register_should_make_the_implementation_resolvable_through_the_built_resolver()
    {
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.Register(typeof(ServiceContract), typeof(Service));

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        var resolved = resolver.Resolve(typeof(ServiceContract)).Should().BeAssignableTo<ServiceContract>().Subject;
        resolved.Describe().Should().Be(nameof(Service), "the registered implementation is what the resolver hands back");
    }

    [Fact]
    public void RegisterInstance_should_return_the_very_same_instance()
    {
        var instance = new Service();
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.RegisterInstance(typeof(ServiceContract), instance);

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        resolver.Resolve(typeof(ServiceContract)).Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterLazy_should_defer_the_factory_until_the_service_is_resolved()
    {
        var invocations = 0;
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.RegisterLazy(typeof(ServiceContract),
                               () =>
                               {
                                   invocations++;

                                   return new Service();
                               });

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();
        invocations.Should().Be(0, "building the resolver must not instantiate the service");

        resolver.Resolve(typeof(ServiceContract)).Should().BeOfType<Service>();
        invocations.Should().Be(1);
    }

    [Fact]
    public void RegisterLazy_should_reject_a_null_factory()
    {
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());

        var act = () => registrar.RegisterLazy(typeof(ServiceContract), null);

        act.Should().Throw<ArgumentNullException>();
    }

    private static HostBuilder CreateHostBuilder() => new();

    private class ServiceContract
    {
        public virtual string Describe() => nameof(ServiceContract);
    }

    private sealed class Service : ServiceContract
    {
        public override string Describe() => nameof(Service);
    }
}

/// <summary>
///     Cover for the resolver, whose contract is to answer <see langword="null" /> rather than throw for anything
///     it cannot supply, and to own the lifetime of the host it was given.
/// </summary>
public class DependencyInjectionTypeResolverTests
{
    [Fact]
    public void Resolve_should_return_null_for_a_null_type()
    {
        using var resolver = new DependencyInjectionTypeResolver(new HostBuilder().Build());

        resolver.Resolve(null).Should().BeNull();
    }

    [Fact]
    public void Resolve_should_return_null_for_an_unregistered_type()
    {
        using var resolver = new DependencyInjectionTypeResolver(new HostBuilder().Build());

        resolver.Resolve(typeof(DependencyInjectionTypeResolverTests)).Should().BeNull();
    }

    [Fact]
    public void Constructor_should_reject_a_null_host()
    {
        var act = () => new DependencyInjectionTypeResolver(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dispose_should_dispose_the_underlying_host()
    {
        var host = new HostBuilder().ConfigureServices(services => services.AddSingleton<TrackingDisposable>()).Build();
        var resolver = new DependencyInjectionTypeResolver(host);
        var disposable = resolver.Resolve(typeof(TrackingDisposable)).Should().BeOfType<TrackingDisposable>().Subject;

        resolver.Dispose();

        disposable.IsDisposed.Should().BeTrue("the resolver owns the host and must release the services it created");
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
