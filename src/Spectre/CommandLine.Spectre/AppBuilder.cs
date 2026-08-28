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
public class AppBuilder : IDisposable
{
    private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _appConfigurationConfigurators = [];
    private readonly ConsoleAppInfo _appInfo;

    /// <summary>The handler installed by <see cref="Create" />, or <see langword="null" /> when this builder did not install one.</summary>
    private readonly ConsoleCancelEventHandler? _cancelKeyPressHandler;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Action<IHostBuilder>> _hostBuilderConfigurators = [];

    /// <summary>Whether <see cref="_cancellationTokenSource" /> was created here and is therefore ours to dispose.</summary>
    private readonly bool _ownsCancellationTokenSource;

    private readonly List<Action<HostBuilderContext, IServiceCollection>> _serviceCollectionConfigurators = [];
    private readonly HashSet<IServicesBundle> _servicesBundles = new() { new AppServicesBundle() };
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AppBuilder" /> class around a caller-supplied cancellation source.
    /// </summary>
    /// <param name="appInfo">Metadata describing the application being built.</param>
    /// <param name="cancellationTokenSource">
    ///     The cancellation source to publish to the application's services. It remains the caller's to dispose —
    ///     <see cref="Dispose()" /> leaves it alone. Use <see cref="Create" /> to have the builder own one instead.
    /// </param>
    public AppBuilder(ConsoleAppInfo appInfo, CancellationTokenSource cancellationTokenSource)
        : this(appInfo, cancellationTokenSource, cancelKeyPressHandler: null, ownsCancellationTokenSource: false)
    { }

    private AppBuilder(ConsoleAppInfo appInfo,
                       CancellationTokenSource cancellationTokenSource,
                       ConsoleCancelEventHandler? cancelKeyPressHandler,
                       bool ownsCancellationTokenSource)
    {
        _appInfo = appInfo;
        _cancellationTokenSource = cancellationTokenSource;
        _cancelKeyPressHandler = cancelKeyPressHandler;
        _ownsCancellationTokenSource = ownsCancellationTokenSource;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="AppBuilder" /> class with the specified arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to initialize the application.</param>
    /// <returns>A new instance of <see cref="AppBuilder" />.</returns>
    /// <remarks>
    ///     The returned builder owns both the cancellation source it creates and the <c>Console.CancelKeyPress</c>
    ///     handler it installs, and releases them on <see cref="Dispose()" />. Dispose it once the application has
    ///     finished running — the cancellation token stays live for the whole run, so an earlier scope exit would
    ///     tear down the application it is meant to be shutting down.
    /// </remarks>
    public static AppBuilder Create(params IEnumerable<string> args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            // The delegate is held rather than re-converted so Dispose can unsubscribe this exact instance.
            // Console.CancelKeyPress is a process-wide event: left subscribed, it pins the source and the
            // closure for the life of the process, and every further Create call adds another handler on top.
            ConsoleCancelEventHandler cancelKeyPressHandler = OnCancelKeyPress;

            Console.CancelKeyPress += cancelKeyPressHandler;

            return new(new(args), cancellationTokenSource, cancelKeyPressHandler, ownsCancellationTokenSource: true);

            void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
            {
                AnsiConsole.WriteLine("Shutting down...");

                // Ctrl+C is raised on the console's own thread and can land while Dispose runs on the main
                // one. The source is then already gone, so there is nothing left to cancel - but the press
                // was user-initiated and still gets an answer.
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    AnsiConsole.WriteLine("Shutdown already in progress.");
                }

                e.Cancel = true;
            }
        }
        catch
        {
            // Nothing has taken ownership yet, so the source would otherwise leak on a failed construction.
            cancellationTokenSource.Dispose();

            throw;
        }
    }

    /// <summary>
    ///     Releases the cancellation source and the <c>Console.CancelKeyPress</c> handler this builder owns.
    /// </summary>
    /// <remarks>
    ///     A builder created through <see cref="Create" /> owns both and releases both. A builder constructed with
    ///     <see cref="AppBuilder(ConsoleAppInfo, CancellationTokenSource)" /> owns neither, so disposing it is a no-op
    ///     and the caller's cancellation source is left intact.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Builds and configures the command-line application.
    /// </summary>
    /// <param name="configurator">Command line application configurator used to configure the commands and options.</param>
    /// <returns>An instance of <see cref="ICommandAppExecutor" /> allowing execution of the app.</returns>
    public ICommandAppExecutor ConfigureCommandApp(Action<IConfigurator> configurator)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _appInfo.Validate();
        _appInfo.PrintAppInfo();
        var builder = Host.CreateDefaultBuilder(_appInfo.Args?.ToArray());

        builder.ConfigureServices((context, services) =>
                                  {
                                      InitializeBundles(services, context);

                                      foreach (var servicesConfigurator in _serviceCollectionConfigurators)
                                      {
                                          servicesConfigurator(context, services);
                                      }

                                      services.AddSingleton(_cancellationTokenSource);
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
        _appInfo.Description = description;

        return this;
    }

    /// <summary>
    ///     Sets the name of the application.
    /// </summary>
    /// <param name="name">The name of the application.</param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    public AppBuilder WithName(string name)
    {
        _appInfo.Name = name;

        return this;
    }

    /// <summary>
    ///     Sets the version of the application.
    /// </summary>
    /// <param name="version">The version of the application.</param>
    /// <returns>The current instance of <see cref="AppBuilder" /> for method chaining.</returns>
    public AppBuilder WithVersion(Version version)
    {
        _appInfo.Version = version;

        return this;
    }

    /// <summary>
    ///     Releases the resources this builder owns.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from a
    ///     finalizer, in which case the managed resources below must not be touched.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (_cancelKeyPressHandler is not null)
            {
                Console.CancelKeyPress -= _cancelKeyPressHandler;
            }

            // Only a source this builder created. One handed in through the public constructor belongs to the
            // caller and may still be in use after the builder is gone.
            if (_ownsCancellationTokenSource)
            {
                _cancellationTokenSource.Dispose();
            }
        }

        _disposed = true;
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
