using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.Common;
using Spectre.Console.Cli;

namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.DependencyInjection;

/// <summary>
///     Provides an implementation of Spectre.Console's <see cref="ITypeRegistrar" /> that integrates with
///     Microsoft.Extensions.DependencyInjection for dependency injection.
/// </summary>
/// <param name="builder">The host builder used to configure services.</param>
public sealed class DependencyInjectionTypeRegistrar(IHostBuilder builder) : ITypeRegistrar
{
    /// <summary>
    ///     Builds and returns a type resolver that can resolve types from the configured service provider.
    /// </summary>
    /// <returns>An <see cref="ITypeResolver" /> implementation that resolves types from the built host.</returns>
    public ITypeResolver Build() => new DependencyInjectionTypeResolver(builder.Build());

    /// <summary>
    ///     Registers a service type with its corresponding implementation type.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The implementation type that will be instantiated to provide the service.</param>
    public void Register(Type service, Type implementation)
    {
        builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
    }

    /// <summary>
    ///     Registers an existing instance as a service.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The existing implementation instance to register.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
    }

    /// <summary>
    ///     Registers a factory function that will be invoked to create an instance of the service when needed.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="func">The factory function that creates the service instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func" /> is null.</exception>
    public void RegisterLazy(Type service, Func<object>? func)
    {
        func.NotNull();

        builder.ConfigureServices((_, services) => services.AddSingleton(service, _ => func()));
    }
}
