using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.Common.ArgumentChecking;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.DependencyInjection;

/// <summary>
///     Provides an implementation of Spectre.Console's <see cref="ITypeRegistrar" /> that integrates with
///     Microsoft.Extensions.DependencyInjection for dependency injection.
/// </summary>
/// <remarks>
///     <para>
///         Lifetimes follow one rule. Everything registered through <see cref="Register" /> — which is how Spectre
///         registers every command, settings type, type converter and pair deconstructor — is <b>transient</b>: a
///         new instance is created for every resolution, so a command holds its transient dependencies for its own
///         lifetime rather than the host's. Dependency lifetimes are unchanged, so singleton services stay shared.
///         <see cref="RegisterInstance" /> and
///         <see cref="RegisterLazy" /> are <b>singletons</b>, because that is what Spectre's contract asks for: the
///         first must hand back the very instance it was given, and the second describes a lazily created instance
///         whose factory runs at most once per host.
///     </para>
///     <para>
///         The infrastructure the library registers itself — the console, the <c>IOutput</c>, the message formatter
///         processor and the application's <see cref="CancellationTokenSource" /> — stays singleton by design: those
///         are shared or stateless, and a command is meant to receive the same instance the application uses.
///     </para>
///     <para>
///         Spectre registers its types when the application runs, after every <c>ConfigureServices</c> delegate, and
///         the last registration of a service type wins. Registering a command type as a singleton from
///         <c>ConfigureServices</c> therefore has no effect; state that must outlive one resolution belongs in a
///         singleton service the command depends on.
///     </para>
/// </remarks>
/// <param name="builder">The host builder used to configure services.</param>
public sealed class DependencyInjectionTypeRegistrar(IHostBuilder builder) : ITypeRegistrar
{
    /// <summary>
    ///     Builds and returns a type resolver that can resolve types from the configured service provider.
    /// </summary>
    /// <returns>An <see cref="ITypeResolver" /> implementation that resolves types from the built host.</returns>
    /// <remarks>
    ///     The host is built here but not started, so an <see cref="IHostedService" /> registered with it has neither
    ///     <c>StartAsync</c> nor <c>StopAsync</c> invoked by the library; the returned resolver disposes the host. See
    ///     the remarks on <see cref="AppBuilder.ConfigureCommandApp" />.
    /// </remarks>
    public ITypeResolver Build() => new DependencyInjectionTypeResolver(builder.Build());

    /// <summary>
    ///     Registers a service type with its corresponding implementation type as a <b>transient</b> service, so
    ///     every resolution creates a new instance.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The implementation type that will be instantiated to provide the service.</param>
    public void Register(Type service, Type implementation)
    {
        builder.ConfigureServices((_, services) => services.AddTransient(service, implementation));
    }

    /// <summary>
    ///     Registers an existing instance as a <b>singleton</b> service, so every resolution returns that very
    ///     instance, as Spectre's contract requires.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The existing implementation instance to register.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
    }

    /// <summary>
    ///     Registers a factory function that will be invoked to create an instance of the service when needed. The
    ///     result is a lazily created <b>singleton</b>: the factory runs on the first resolution and every later
    ///     resolution returns the same instance.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="factory">The factory function that creates the service instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory" /> is null.</exception>
    public void RegisterLazy(Type service, Func<object>? factory)
    {
        factory.NotNull();

        builder.ConfigureServices((_, services) => services.AddSingleton(service, _ => factory()));
    }
}
