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

    /// <summary>
    ///     Non-<see langword="null" /> exactly when this builder created the cancellation source and is therefore
    ///     the one to dispose it. Also the gate that keeps that disposal from overlapping the interrupt handler's
    ///     cancellation.
    /// </summary>
    private readonly InterruptGate? _interruptGate;

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
        : this(appInfo, cancellationTokenSource, cancelKeyPressHandler: null, interruptGate: null)
    { }

    private AppBuilder(ConsoleAppInfo appInfo,
                       CancellationTokenSource cancellationTokenSource,
                       ConsoleCancelEventHandler? cancelKeyPressHandler,
                       InterruptGate? interruptGate)
    {
        _appInfo = appInfo;
        _cancellationTokenSource = cancellationTokenSource;
        _cancelKeyPressHandler = cancelKeyPressHandler;
        _interruptGate = interruptGate;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="AppBuilder" /> class with the specified arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to initialize the application.</param>
    /// <returns>A new instance of <see cref="AppBuilder" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This also installs a <see cref="Console.CancelKeyPress" /> handler and creates the
    ///         <see cref="CancellationTokenSource" /> the application cancels through. The first Ctrl+C cancels that
    ///         source cooperatively, so a command honouring its <see cref="CancellationToken" /> can stop and tidy up;
    ///         a second Ctrl+C takes the default path and terminates the process, so a command that ignores its token
    ///         never leaves the application unkillable from the keyboard.
    ///     </para>
    ///     <para>
    ///         The source is registered in the container, so a command can resolve it to request shutdown itself.
    ///     </para>
    ///     <para>
    ///         The returned builder owns both the source it creates and the handler it installs, and releases them on
    ///         <see cref="Dispose()" />. Dispose it once the application has finished running — the cancellation token
    ///         stays live for the whole run, so an earlier scope exit would tear down the application it is meant to
    ///         be shutting down. The handler also detaches itself once an interrupt has been handled, so an
    ///         interrupted application releases the subscription without waiting for disposal.
    ///     </para>
    /// </remarks>
    public static AppBuilder Create(params IEnumerable<string> args)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        // Declared out here so the catch below can unsubscribe it. Nothing has taken ownership until the
        // builder is constructed, so a throw after the subscription would otherwise leave a handler on a
        // process-wide event holding a disposed source - one that answers a later Ctrl+C by suppressing
        // nothing and cancelling nothing.
        ConsoleCancelEventHandler? cancelKeyPressHandler = null;
        try
        {
            var interruptHandled = 0;
            var interruptGate = new InterruptGate();

            // The delegate is held rather than re-converted so Dispose can unsubscribe this exact instance.
            // Console.CancelKeyPress is a process-wide event: left subscribed, it pins the source and the
            // closure for the life of the process, and every further Create call adds another handler on top.
            cancelKeyPressHandler = OnCancelKeyPress;

            Console.CancelKeyPress += cancelKeyPressHandler;

            return new(new(args), cancellationTokenSource, cancelKeyPressHandler, interruptGate);

            // The handler also detaches itself, so the first interrupt is handled cooperatively and a second one
            // takes the default path and terminates the process. Suppressing every interrupt would leave the
            // application unkillable from the keyboard whenever the running command does not observe its token --
            // a blocking call, or a third-party library in a tight loop. Detaching here and in Dispose is not a
            // conflict: removing a handler that is already gone is a no-op, so whichever happens first wins, and
            // an application that simply runs to completion still releases the subscription.
            //
            // Unsubscribing by method group rather than through cancelKeyPressHandler is deliberate: it keeps the
            // handler independent of when that variable is assigned. Both conversions name the same method and
            // close over the same locals, and delegate equality is by target and method rather than by reference,
            // so -= matches the instance that was added.
            void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
            {
                // Unsubscribing inside the handler stops future raises, but it cannot remove this delegate from an
                // invocation list a concurrent raise has already captured. Without this one-shot guard two interrupts
                // dispatched close together could both suppress termination, and the second press promised above
                // would need a third.
                //
                // Returning without touching e.Cancel is deliberate. The event arguments are one instance shared by
                // every subscriber on this raise and the runtime reads whatever is left after the last handler
                // returns, so writing false here would override a suppression some other subscriber legitimately
                // asked for. False is already the default; this handler only ever adds a true of its own.
                if (Interlocked.Exchange(ref interruptHandled, 1) != 0)
                {
                    return;
                }

                Console.CancelKeyPress -= OnCancelKeyPress;

                lock (interruptGate.Sync)
                {
                    if (interruptGate.SourceReleased)
                    {
                        // The run this handler exists to interrupt is already over. Leave the press to the default
                        // path: one that cancels nothing and blocks termination too would do nothing at all.
                        return;
                    }

                    interruptGate.CancelInProgress = true;
                }

                string? callbackFailure = null;

                // Cancel runs OUTSIDE the gate deliberately. It invokes consumer cancellation callbacks
                // synchronously, and a callback is free to dispose this builder on this very thread. Holding a
                // re-entrant lock across the call would not stop that: the nested Dispose would simply re-acquire
                // the lock it already owns and tear the source down inside the Cancel still unwinding - the exact
                // overlap the gate exists to prevent. Instead the call is flagged as in progress, and a Dispose
                // arriving during it defers the release to us.
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (AggregateException exception)
                {
                    // Cancel() wraps anything the callbacks throw. An unhandled exception on this thread terminates
                    // the process -- the exact opposite of the graceful shutdown being requested. Reported rather
                    // than swallowed, but only the message: a stack trace is noise while the application is already
                    // on its way out. Cancellation was still requested, so the interrupt is still handled.
                    callbackFailure = exception.Message;
                }
                finally
                {
                    lock (interruptGate.Sync)
                    {
                        interruptGate.CancelInProgress = false;

                        // A Dispose that arrived while Cancel was on the stack left the source for this thread to
                        // release, now that nothing is running against it.
                        if (interruptGate.DisposeDeferred && !interruptGate.SourceReleased)
                        {
                            interruptGate.SourceReleased = true;
                            cancellationTokenSource.Dispose();
                        }
                    }
                }

                // Announced only after cancellation has actually been requested, and off the gate: this runs on the
                // CancelKeyPress thread, where console I/O can block, and nothing should wait behind it.
                e.Cancel = true;

                if (callbackFailure is not null)
                {
                    AnsiConsole.WriteLine($"A cancellation callback failed during shutdown: {callbackFailure}");

                    return;
                }

                AnsiConsole.WriteLine("Shutting down... press Ctrl+C again to force an exit.");
            }
        }
        catch
        {
            // Nothing has taken ownership yet, so both the subscription and the source would otherwise leak
            // on a failed construction. Removing a handler that was never added is a no-op.
            if (cancelKeyPressHandler is not null)
            {
                Console.CancelKeyPress -= cancelKeyPressHandler;
            }

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

        return new CommandAppExecutor(app, _cancellationTokenSource.Token);
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

            // Only a source this builder created - the gate exists exactly then. One handed in through the public
            // constructor belongs to the caller and may still be in use after the builder is gone.
            //
            // Released under the same gate the interrupt handler cancels through, because Dispose may not overlap
            // Cancel. Unsubscribing above stops new raises but cannot recall one already in flight.
            if (_interruptGate is not null)
            {
                lock (_interruptGate.Sync)
                {
                    if (_interruptGate.CancelInProgress)
                    {
                        // Cancel() is on the stack - possibly on this very thread, reached through a cancellation
                        // callback that disposed the builder. Disposing now would tear the source down inside the
                        // call still unwinding, so the cancelling thread releases it instead. Deferring rather than
                        // waiting also keeps Dispose from blocking on a consumer callback that never returns.
                        _interruptGate.DisposeDeferred = true;
                    }
                    else if (!_interruptGate.SourceReleased)
                    {
                        _interruptGate.SourceReleased = true;
                        _cancellationTokenSource.Dispose();
                    }
                }
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

    /// <summary>
    ///     Serialises the interrupt handler's cancellation against disposal of the source it cancels.
    /// </summary>
    /// <remarks>
    ///     <see cref="CancellationTokenSource" /> documents every public member as thread-safe <em>except</em>
    ///     <see cref="CancellationTokenSource.Dispose()" />, which "must only be used when all other operations have
    ///     completed". The interrupt handler runs on the console's own thread and can therefore call
    ///     <see cref="CancellationTokenSource.Cancel()" /> while <see cref="AppBuilder.Dispose()" /> runs on the main
    ///     one.
    ///     <para>
    ///         Rather than hold a lock across <c>Cancel()</c>, the call is flagged as in progress and disposal is
    ///         deferred to whoever is cancelling. Holding a lock would not work: consumer cancellation callbacks run
    ///         synchronously and one may dispose the builder on the cancelling thread, where a re-entrant lock is
    ///         simply re-acquired and the source torn down inside the call still unwinding. Deferring also stops
    ///         <see cref="AppBuilder.Dispose()" /> blocking behind a consumer callback that never returns.
    ///     </para>
    /// </remarks>
    private sealed class InterruptGate
    {
        /// <summary>Gets the gate the state below is read and written under.</summary>
        public Lock Sync { get; } = new();

        /// <summary>Gets or sets a value indicating whether <c>Cancel()</c> is currently on the stack.</summary>
        public bool CancelInProgress { get; set; }

        /// <summary>Gets or sets a value indicating whether a disposal arrived during cancellation and still owes a release.</summary>
        public bool DisposeDeferred { get; set; }

        /// <summary>Gets or sets a value indicating whether the source has been disposed.</summary>
        public bool SourceReleased { get; set; }
    }
}
