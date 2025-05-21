using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.DependencyInjection;

/// <summary>
///     Provides a type resolver implementation that resolves types using dependency injection through an
///     <see cref="IHost" /> instance.
/// </summary>
/// <param name="provider">The host provider used to resolve dependencies.</param>
/// <exception cref="ArgumentNullException">Thrown when the provider is null.</exception>
public sealed class DependencyInjectionTypeResolver(IHost provider) : ITypeResolver, IDisposable
{
    private readonly IHost _host = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    ///     Disposes the underlying host provider.
    /// </summary>
    public void Dispose() => _host.Dispose();

    /// <summary>
    ///     Resolves a service of the specified type from the service provider.
    /// </summary>
    /// <param name="type">The type of service to resolve.</param>
    /// <returns>The resolved service instance, or null if the type is null or the service cannot be resolved.</returns>
    public object? Resolve(Type? type) => type != null ? _host.Services.GetService(type) : null;
}
