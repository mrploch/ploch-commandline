using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Spectre.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Tests.DependencyInjection;

/// <summary>
///     Cover for the bridge between Spectre.Console.Cli's type registry and Microsoft.Extensions.DependencyInjection.
///     Every registration made here has to be visible from the resolver the registrar builds, with the lifetime the
///     registrar promises: <c>Register</c> is transient, <c>RegisterInstance</c> and <c>RegisterLazy</c> are
///     singletons (PLO-781).
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
    public void Register_should_create_a_new_instance_for_every_resolution()
    {
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.Register(typeof(ServiceContract), typeof(Service));

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        var first = resolver.Resolve(typeof(ServiceContract));
        var second = resolver.Resolve(typeof(ServiceContract));

        first.Should().NotBeSameAs(second, "a type Spectre registers - a command or a settings type - is transient, so two resolutions never share state");
    }

    [Fact]
    public void Register_should_give_each_resolution_a_fresh_transient_dependency()
    {
        var hostBuilder = CreateHostBuilder();
        hostBuilder.ConfigureServices(services => services.AddTransient<Dependency>());
        var registrar = new DependencyInjectionTypeRegistrar(hostBuilder);
        registrar.Register(typeof(ServiceWithDependency), typeof(ServiceWithDependency));

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        var first = resolver.Resolve(typeof(ServiceWithDependency)).Should().BeOfType<ServiceWithDependency>().Subject;
        var second = resolver.Resolve(typeof(ServiceWithDependency)).Should().BeOfType<ServiceWithDependency>().Subject;

        first.Dependency.Should().NotBeSameAs(second.Dependency, "a transient dependency must not be captured for the life of the host by the command that consumes it");
        first.Dependency.Id.Should().NotBe(second.Dependency.Id);
    }

    [Fact]
    public void Register_should_take_precedence_over_an_earlier_singleton_registration_of_the_same_service()
    {
        var hostBuilder = CreateHostBuilder();
        hostBuilder.ConfigureServices(services => services.AddSingleton<ServiceContract, Service>());
        var registrar = new DependencyInjectionTypeRegistrar(hostBuilder);
        registrar.Register(typeof(ServiceContract), typeof(Service));

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        var first = resolver.Resolve(typeof(ServiceContract));
        var second = resolver.Resolve(typeof(ServiceContract));

        first.Should()
             .NotBeSameAs(second, "Spectre registers its types after every ConfigureServices delegate and the last registration wins, so a command cannot be pinned as a singleton from ConfigureServices");
    }

    [Fact]
    public void Register_should_let_the_resolver_dispose_the_instances_it_created()
    {
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.Register(typeof(DisposableService), typeof(DisposableService));
        var resolver = (DependencyInjectionTypeResolver)registrar.Build();
        var service = resolver.Resolve(typeof(DisposableService)).Should().BeOfType<DisposableService>().Subject;

        resolver.Dispose();

        service.IsDisposed.Should().BeTrue("the root provider tracks the transient disposables it creates and releases them with the host Spectre disposes after a run");
    }

    [Fact]
    public void RegisterInstance_should_return_the_very_same_instance_on_every_resolution()
    {
        var instance = new Service();
        var registrar = new DependencyInjectionTypeRegistrar(CreateHostBuilder());
        registrar.RegisterInstance(typeof(ServiceContract), instance);

        using var resolver = (DependencyInjectionTypeResolver)registrar.Build();

        resolver.Resolve(typeof(ServiceContract)).Should().BeSameAs(instance);
        resolver.Resolve(typeof(ServiceContract)).Should().BeSameAs(instance, "Spectre's contract is that an instance registration hands back the instance it was given");
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
    public void RegisterLazy_should_invoke_the_factory_once_and_share_the_result()
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

        var first = resolver.Resolve(typeof(ServiceContract));
        var second = resolver.Resolve(typeof(ServiceContract));

        invocations.Should().Be(1, "a lazy registration is a lazily created singleton, not a factory invoked per resolution");
        first.Should().BeSameAs(second);
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

    private sealed class Dependency
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ServiceWithDependency(Dependency dependency)
    {
        public Dependency Dependency => dependency;
    }

    private sealed class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
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
