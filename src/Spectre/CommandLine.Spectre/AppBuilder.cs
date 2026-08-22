using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Spectre.Configuration;
using Ploch.CommandLine.Spectre.DependencyInjection;
using Ploch.Common.ArgumentChecking;
using Ploch.Common.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Provides a builder for configuring and constructing a command-line application.
/// </summary>
/// <remarks>
///     The <see cref="AppBuilder" /> class allows for the creation and customization of a command-line application
///     by specifying its name, description, version, and other configurations. It integrates with the Spectre.Console.Cli
///     library for command-line interface functionality and Microsoft.Extensions.Hosting for dependency injection and
///     configuration.
/// </remarks>
public class AppBuilder(ConsoleAppInfo appInfo, CancellationTokenSource cancellationTokenSource)
{
    private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _appConfigurationConfigurators = [];
    private readonly List<Action<IHostBuilder>> _hostBuilderConfigurators = [];
    private readonly List<Action<HostBuilderContext, IServiceCollection>> _serviceCollectionConfigurators = [];
    private readonly HashSet<IServicesBundle> _servicesBundles = new() { new AppServicesBundle() };

    /// <summary>
    ///     Creates a new instance of the <see cref="AppBuilder" /> class with the specified arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to initialize the application.</param>
    /// <returns>A new instance of <see cref="AppBuilder" />.</returns>
    public static AppBuilder Create(params IEnumerable<string> args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
                                  {
                                      AnsiConsole.WriteLine("Shutting down...");
                                      cts.Cancel();
                                      e.Cancel = true;
                                  };

        return new(new(args), cts);
    }

    /// <summary>
    ///     Builds and configures the command-line application.
    /// </summary>
    /// <param name="configurator">Command line application configurator used to configure the commands and options.</param>
    /// <returns>An instance of <see cref="ICommandAppExecutor" /> allowing execution of the app.</returns>
    public ICommandAppExecutor ConfigureCommandApp(Action<IConfigurator> configurator)
    {
        appInfo.Validate();
        appInfo.PrintAppInfo();
        var builder = Host.CreateDefaultBuilder(appInfo.Args?.ToArray());

        builder.ConfigureServices((context, services) =>
                                  {
                                      InitializeBundles(services, context);

                                      foreach (var servicesConfigurator in _serviceCollectionConfigurators)
                                      {
                                          servicesConfigurator(context, services);
                                      }

                                      services.AddSingleton(cancellationTokenSource);
                                  });
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
                                          {
                                              configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                                              foreach (var appConfigurationConfigurator in _appConfigurationConfigurators)
                                              {
                                                  appConfigurationConfigurator(context, configurationBuilder);
                                              }
                                          });

        // Add services to the container
        foreach (var hostBuilderConfigurator in _hostBuilderConfigurators)
        {
            hostBuilderConfigurator(builder);
        }

        var registrar = new DependencyInjectionTypeRegistrar(builder);

        var app = new CommandApp(registrar);

        app.Configure(configurator);

        return new CommandAppExecutor(app);
    }

    /// <summary>
    ///     Configures the application's configuration using the specified delegate.
    /// </summary>
    /// <param name="appConfigurationConfigurator">
    ///     A delegate that provides access to the <see cref="IConfigurationBuilder" /> for configuring the application's
    ///     configuration.
    /// </param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appConfigurationConfigurator" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method simplifies the customization of the application's configuration by allowing direct access to the
    ///     <see cref="IConfigurationBuilder" />. It can be used to add configuration sources, modify existing configurations,
    ///     or apply specific settings without requiring access to the hosting context.
    ///     <para>
    ///         Calls accumulate: every delegate passed to either overload is applied, in the order it was added, matching
    ///         the additive behaviour of <see cref="IHostBuilder.ConfigureAppConfiguration" />.
    ///     </para>
    /// </remarks>
    [SuppressMessage("ReSharper",
                     "UnusedMember.Global",
                     Justification = "This method is a part of the public API and is intended for use by consumers of the AppBuilder class.")]
    public AppBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> appConfigurationConfigurator)
    {
        appConfigurationConfigurator.NotNull();

        return ConfigureAppConfiguration((_, builder) => appConfigurationConfigurator(builder));
    }

    /// <summary>
    ///     Configures the application's configuration using the specified delegate.
    /// </summary>
    /// <param name="appConfigurationConfigurator">
    ///     A delegate that provides access to the <see cref="HostBuilderContext" /> and
    ///     <see cref="IConfigurationBuilder" /> for configuring the application's configuration.
    /// </param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appConfigurationConfigurator" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method allows for advanced customization of the application's configuration by enabling
    ///     the use of both the hosting context and the configuration builder. It can be used to add
    ///     configuration sources, modify existing configurations, or apply environment-specific settings.
    ///     <para>
    ///         Calls accumulate: every delegate passed to either overload is applied, in the order it was added, matching
    ///         the additive behaviour of <see cref="IHostBuilder.ConfigureAppConfiguration" />.
    ///     </para>
    /// </remarks>
    [SuppressMessage("ReSharper",
                     "MemberCanBePrivate.Global",
                     Justification = "This method is a part of the public API and is intended for use by consumers of the AppBuilder class.")]
    public AppBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> appConfigurationConfigurator)
    {
        _appConfigurationConfigurators.Add(appConfigurationConfigurator.NotNull());

        return this;
    }

    /// <summary>
    ///     Configures the host builder for the application.
    /// </summary>
    /// <param name="configureDelegate">
    ///     A delegate that provides custom configuration for the <see cref="IHostBuilder" />.
    /// </param>
    /// <returns>
    ///     The current instance of <see cref="AppBuilder" /> for method chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureDelegate" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method allows customization of the application's host builder, enabling the addition
    ///     of services, configuration, and other host-level settings. It integrates with the
    ///     <see cref="Microsoft.Extensions.Hosting" /> framework.
    ///     <para>Calls accumulate: every delegate is applied to the host builder, in the order it was added.</para>
    /// </remarks>
    [SuppressMessage("ReSharper",
                     "UnusedMember.Global",
                     Justification = "This method is a part of the public API and is intended for use by consumers of the AppBuilder class.")]
    public AppBuilder ConfigureHost(Action<IHostBuilder> configureDelegate)
    {
        _hostBuilderConfigurators.Add(configureDelegate.NotNull());

        return this;
    }

    /// <summary>
    ///     Configures the services for the application.
    /// </summary>
    /// <param name="servicesConfigurator">
    ///     An action to configure the <see cref="IServiceCollection" /> for dependency injection.
    /// </param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="servicesConfigurator" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method allows customization of the application's services by providing a delegate
    ///     that operates on the <see cref="IServiceCollection" />. It is useful for registering
    ///     dependencies and configuring services required by the application.
    ///     <para>
    ///         Calls accumulate: every delegate passed to either overload is applied, in the order it was added, matching
    ///         the additive behaviour of <see cref="IHostBuilder.ConfigureServices" />.
    ///     </para>
    /// </remarks>
    [SuppressMessage("ReSharper",
                     "UnusedMember.Global",
                     Justification = "This method is a part of the public API and is intended for use by consumers of the AppBuilder class.")]
    public AppBuilder ConfigureServices(Action<IServiceCollection> servicesConfigurator)
    {
        servicesConfigurator.NotNull();

        return ConfigureServices((_, services) => servicesConfigurator(services));
    }

    /// <summary>
    ///     Configures the services for the application using the specified configurator.
    /// </summary>
    /// <param name="servicesConfigurator">
    ///     An action that allows customization of the service collection. The action provides access to the
    ///     <see cref="HostBuilderContext" /> and <see cref="IServiceCollection" /> for configuring services.
    /// </param>
    /// <returns>The current instance of <see cref="AppBuilder" /> to allow method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="servicesConfigurator" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method enables the addition or modification of services in the application's dependency injection container.
    ///     It supports advanced configuration scenarios by providing access to the hosting context.
    ///     <para>
    ///         Calls accumulate: every delegate passed to either overload is applied, in the order it was added, matching
    ///         the additive behaviour of <see cref="IHostBuilder.ConfigureServices" />.
    ///     </para>
    /// </remarks>
    public AppBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> servicesConfigurator)
    {
        _serviceCollectionConfigurators.Add(servicesConfigurator.NotNull());

        return this;
    }

    /// <summary>
    ///     Registers a services bundle to be configured when the application is built.
    /// </summary>
    /// <typeparam name="TServicesBundle">The bundle type to register. Must expose a parameterless constructor.</typeparam>
    /// <returns>The same <see cref="AppBuilder" /> instance, to allow chaining.</returns>
    public AppBuilder AddServicesBundle<TServicesBundle>() where TServicesBundle : IServicesBundle, new()
    {
        _servicesBundles.Add(new TServicesBundle());

        return this;
    }

    /// <summary>
    ///     Sets the description of the application.
    /// </summary>
    /// <param name="description">The description of the application.</param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    public AppBuilder WithDescription(string description)
    {
        appInfo.Description = description;

        return this;
    }

    /// <summary>
    ///     Sets the name of the application.
    /// </summary>
    /// <param name="name">The name of the application.</param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    public AppBuilder WithName(string name)
    {
        appInfo.Name = name;

        return this;
    }

    /// <summary>
    ///     Sets the version of the application.
    /// </summary>
    /// <param name="version">The version of the application.</param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    public AppBuilder WithVersion(Version version)
    {
        appInfo.Version = version;

        return this;
    }

    private void InitializeBundles(IServiceCollection services, HostBuilderContext context)
    {
        if (_servicesBundles.Count > 0)
        {
            foreach (var servicesBundle in _servicesBundles)
            {
                services.AddServicesBundle(servicesBundle, context.Configuration);
            }
        }
    }
}
