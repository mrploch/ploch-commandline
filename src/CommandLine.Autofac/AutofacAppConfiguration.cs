using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine.Autofac;

/// <summary>
///     Provides extension methods for configuring an <see cref="AppBuilder" /> to use Autofac as the dependency injection
///     provider.
/// </summary>
public static class AutofacAppConfiguration
{
    /// <summary>
    ///     Configures the <see cref="AppBuilder" /> to use Autofac as the dependency injection provider.
    /// </summary>
    /// <param name="builder">The <see cref="AppBuilder" /> instance to configure.</param>
    /// <param name="containerBuilder">
    ///     An optional <see cref="ContainerBuilder" /> instance for configuring Autofac. If not
    ///     provided, a new instance will be created.
    /// </param>
    /// <returns>The configured <see cref="AppBuilder" /> instance.</returns>
    public static AppBuilder UseAutofac(this AppBuilder builder, ContainerBuilder? containerBuilder = null)
    {
        builder.Configure(container => container.ServiceProviderFactory = () =>
                                                                          {
                                                                              containerBuilder ??= new ContainerBuilder();
                                                                              containerBuilder.Populate(container.Services);
                                                                              return new AutofacServiceProvider(containerBuilder.Build());
                                                                          });

        return builder;
    }
}