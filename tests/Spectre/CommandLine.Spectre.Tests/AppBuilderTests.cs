using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Ploch.Common.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for the application builder. Every fluent method has to return the same builder so calls chain, and the
///     configuration each one records has to survive into the host that Spectre.Console.Cli resolves commands from —
///     which only happens once a command actually runs.
/// </summary>
/// <remarks>
///     The three "combine every delegate" tests were characterisation tests pinning the builder's original
///     last-call-wins behaviour; issue #22 made the three configuration methods additive, matching
///     <see cref="IHostBuilder" /> and <c>AddServicesBundle</c>, and the tests now pin that instead. The
///     "take precedence ... when called after it" tests pin issue #29: calls of all three kinds share one sequence and
///     are replayed in call order, so the later call wins whichever method made it.
/// </remarks>
[Collection(GlobalConsoleState.Name)]
public sealed class AppBuilderTests : IDisposable
{
    /// <summary>Expected marker order for the additive-configuration tests; a field rather than a literal so CA1861 stays quiet.</summary>
    private static readonly string[] FirstThenSecond = ["first", "second"];

    /// <summary>Expected marker order when the two overloads of the same method are mixed.</summary>
    private static readonly string[] WithoutThenWithContext = ["without-context", "with-context"];

    /// <summary>Expected marker order when <c>ConfigureHost</c> is called before <c>ConfigureServices</c>.</summary>
    private static readonly string[] FromHostThenFromServices = ["from-host", "from-services"];

    /// <summary>Expected marker order when <c>ConfigureServices</c> is called before <c>ConfigureHost</c>.</summary>
    private static readonly string[] FromServicesThenFromHost = ["from-services", "from-host"];

    /// <summary>Expected marker order when calls of every kind are interleaved.</summary>
    private static readonly string[] InterleavedServiceCalls = ["services-1", "host-1", "services-2", "host-2"];

    /// <summary>Expected markers once a call recorded during replay has taken effect.</summary>
    private static readonly string[] LateOnly = ["late"];

    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly RecordingConsole _console = new();

    public AppBuilderTests()
    {
        AnsiConsole.Console = _console.Console;
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());
    }

    public void Dispose()
    {
        ProbeCommand.Recorder = null;
        AnsiConsole.Console = _originalConsole;
        EnvironmentSettings.Reset();
        _console.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WithName_should_set_the_name_used_by_the_start_up_banner()
    {
        var appInfo = new ConsoleAppInfo();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(appInfo, cancellationTokenSource);

        builder.WithName("Widget Tool").Should().BeSameAs(builder, "the fluent methods chain");

        appInfo.Name.Should().Be("Widget Tool");
    }

    [Fact]
    public void WithVersion_should_set_the_version_used_by_the_start_up_banner()
    {
        var appInfo = new ConsoleAppInfo();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(appInfo, cancellationTokenSource);

        builder.WithVersion(new Version(1, 2, 3)).Should().BeSameAs(builder);

        appInfo.Version.Should().Be(new Version(1, 2, 3));
    }

    [Fact]
    public void WithDescription_should_set_the_description_used_by_the_start_up_banner()
    {
        var appInfo = new ConsoleAppInfo();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(appInfo, cancellationTokenSource);

        builder.WithDescription("Does widget things").Should().BeSameAs(builder);

        appInfo.Description.Should().Be("Does widget things");
    }

    [Fact]
    public void ConfigureCommandApp_should_print_the_start_up_banner()
    {
        var recorder = RunProbeCommand(builder => builder.WithName("Widget Tool").WithVersion(new Version(4, 5)).WithDescription("Widget things"));

        recorder.ExitCode.Should().Be(0);
        _console.Output.Should().Contain("Widget Tool 4.5").And.Contain("Widget things");
    }

    [Fact]
    public void ConfigureCommandApp_should_reject_an_application_without_a_name()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo(), cancellationTokenSource);

        var act = () => builder.ConfigureCommandApp(_ => { });

        act.Should().Throw<InvalidOperationException>("the banner renders the name as FigletText, so it cannot be missing");
    }

    [Fact]
    public void ConfigureServices_should_make_the_registered_services_available_to_a_command()
    {
        var marker = new Marker();

        var recorder = RunProbeCommand(builder => builder.ConfigureServices(services => services.AddSingleton(marker)));

        recorder.ExitCode.Should().Be(0);
        recorder.Marker.Should().BeSameAs(marker);
    }

    [Fact]
    public void ConfigureServices_should_expose_the_host_context_to_the_configurator()
    {
        HostBuilderContext? capturedContext = null;

        var recorder = RunProbeCommand(builder => builder.ConfigureServices((context, services) =>
                                                                            {
                                                                                capturedContext = context;
                                                                                services.AddSingleton(new Marker());
                                                                            }));

        recorder.ExitCode.Should().Be(0);
        capturedContext.Should().NotBeNull();
        capturedContext!.Configuration.Should().NotBeNull("the context carries the configuration built so far");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_make_the_added_configuration_visible_to_a_command()
    {
        var recorder = RunProbeCommand(builder =>
                                           builder.ConfigureAppConfiguration(configuration =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:key"] = "from-configuration"
                                                                                     })));

        recorder.ExitCode.Should().Be(0);
        recorder.ConfigurationValue.Should().Be("from-configuration");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_expose_the_host_context_to_the_configurator()
    {
        HostBuilderContext? capturedContext = null;

        var recorder = RunProbeCommand(builder => builder.ConfigureAppConfiguration((context, configuration) =>
                                                                                    {
                                                                                        capturedContext = context;
                                                                                        configuration.AddInMemoryCollection(
                                                                                            new Dictionary<string, string?>
                                                                                            {
                                                                                                ["probe:key"] = "from-context"
                                                                                            });
                                                                                    }));

        recorder.ExitCode.Should().Be(0);
        capturedContext.Should().NotBeNull();
        recorder.ConfigurationValue.Should().Be("from-context");
    }

    [Fact]
    public void ConfigureHost_should_apply_the_delegate_to_the_host_builder()
    {
        var marker = new Marker();

        var recorder = RunProbeCommand(builder =>
                                           builder.ConfigureHost(hostBuilder =>
                                                                     hostBuilder.ConfigureServices(services => services.AddSingleton(marker))));

        recorder.ExitCode.Should().Be(0);
        recorder.Marker.Should().BeSameAs(marker);
    }

    [Fact]
    public void AddServicesBundle_should_register_the_services_the_bundle_configures()
    {
        var recorder = RunProbeCommand(builder => builder.AddServicesBundle<MarkerServicesBundle>());

        recorder.ExitCode.Should().Be(0);
        recorder.Marker.Should().NotBeNull("the bundle registers the marker in the container the command resolves from");
    }

    [Fact]
    public void ConfigureCommandApp_should_register_the_cancellation_token_source_it_was_built_with()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var recorder = RunProbeCommand(_ => { }, cancellationTokenSource);

        recorder.ExitCode.Should().Be(0);
        recorder.CancellationTokenSource.Should().BeSameAs(cancellationTokenSource, "commands cancel the application through this source");
    }

    /// <summary>
    ///     The DI-registration test above passes even if <see cref="AppBuilder.ConfigureCommandApp" /> hands the
    ///     executor an unrelated token, because it only inspects the container. This one drives the real Spectre
    ///     pipeline and asserts on the token the command was actually invoked with, so a regression to
    ///     <c>CancellationToken.None</c> at the executor call site fails here.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_hand_the_running_command_the_token_of_the_source_it_was_built_with()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var recorder = RunProbeCommand(_ => { }, cancellationTokenSource);

        recorder.ExitCode.Should().Be(0);
        recorder.ExecutionToken
                .CanBeCanceled.Should()
                .BeTrue("CancellationToken.None can never be cancelled, which would make the whole feature inert");
        recorder.ExecutionToken.IsCancellationRequested.Should().BeFalse();

        cancellationTokenSource.Cancel();

        recorder.ExecutionToken
                .IsCancellationRequested.Should()
                .BeTrue("the command must observe the very source the application was built with, not a detached one");
    }

    [Fact]
    public void Create_should_feed_the_supplied_arguments_into_the_host_configuration()
    {
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        var builder = AppBuilder.Create("--probe:key=from-command-line").WithName("Widget Tool");

        var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        recorder.ExitCode = executor.Run("probe");

        recorder.ExitCode.Should().Be(0);
        recorder.ConfigurationValue.Should().Be("from-command-line", "Host.CreateDefaultBuilder receives the arguments Create was given");
    }

    [Fact]
    public void ConfigureServices_should_run_every_delegate_it_was_given()
    {
        var recorder = RunProbeCommand(builder =>
                                       {
                                           builder.ConfigureServices(services => services.AddSingleton(new Marker { Name = "first" }));
                                           builder.ConfigureServices(services => services.AddSingleton(new Marker { Name = "second" }));
                                       });

        recorder.ExitCode.Should().Be(0);
        recorder.MarkerNames.Should()
                .Equal(FirstThenSecond, "the builder combines the delegates and runs them in the order they were added");
    }

    [Fact]
    public void ConfigureServices_should_combine_both_overloads_into_the_same_sequence()
    {
        var recorder = RunProbeCommand(builder =>
                                       {
                                           builder.ConfigureServices(services => services.AddSingleton(new Marker { Name = "without-context" }));
                                           builder.ConfigureServices((_, services) => services.AddSingleton(new Marker { Name = "with-context" }));
                                       });

        recorder.ExitCode.Should().Be(0);
        recorder.MarkerNames.Should()
                .Equal(WithoutThenWithContext, "the overload taking the host context records into the same sequence");
    }

    [Fact]
    public void ConfigureHost_should_run_every_delegate_it_was_given()
    {
        var recorder = RunProbeCommand(builder =>
                                       {
                                           builder.ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "first" })));
                                           builder.ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "second" })));
                                       });

        recorder.ExitCode.Should().Be(0);
        recorder.MarkerNames.Should().Equal(FirstThenSecond, "the builder combines the delegates and applies them all to the host builder");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_run_every_delegate_it_was_given()
    {
        var recorder = RunProbeCommand(builder =>
                                       {
                                           builder.ConfigureAppConfiguration(configuration =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:key"] = "from-the-first-call"
                                                                                     }));
                                           builder.ConfigureAppConfiguration(configuration =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:other"] = "from-the-second-call"
                                                                                     }));
                                       });

        recorder.ExitCode.Should().Be(0);
        recorder.SecondConfigurationValue.Should().Be("from-the-second-call");
        recorder.ConfigurationValue.Should().Be("from-the-first-call", "the builder combines the delegates, so both configuration sources are added");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_combine_both_overloads_into_the_same_sequence()
    {
        var recorder = RunProbeCommand(builder =>
                                       {
                                           builder.ConfigureAppConfiguration(configuration =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:key"] = "without-context"
                                                                                     }));
                                           builder.ConfigureAppConfiguration((_, configuration) =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:other"] = "with-context"
                                                                                     }));
                                       });

        recorder.ExitCode.Should().Be(0);
        recorder.ConfigurationValue.Should().Be("without-context");
        recorder.SecondConfigurationValue.Should().Be("with-context", "the overload taking the host context records into the same sequence");
    }

    [Fact]
    public void ConfigureServices_should_take_precedence_over_ConfigureHost_when_called_after_it()
    {
        var recorder = RunProbeCommand(builder => builder
                                                  .ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "from-host" })))
                                                  .ConfigureServices(services => services.AddSingleton(new Marker { Name = "from-services" })));

        recorder.ExitCode.Should().Be(0);
        recorder.Marker!.Name.Should().Be("from-services", "the registration made by the later call is the last one, so it wins");
        recorder.MarkerNames.Should().Equal(FromHostThenFromServices, "the builder replays the calls in the order they were made");
    }

    [Fact]
    public void ConfigureHost_should_take_precedence_over_ConfigureServices_when_called_after_it()
    {
        var recorder = RunProbeCommand(builder => builder
                                                  .ConfigureServices(services => services.AddSingleton(new Marker { Name = "from-services" }))
                                                  .ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "from-host" }))));

        recorder.ExitCode.Should().Be(0);
        recorder.Marker!.Name.Should().Be("from-host", "the registration made by the later call is the last one, so it wins");
        recorder.MarkerNames.Should().Equal(FromServicesThenFromHost, "the builder replays the calls in the order they were made");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_take_precedence_over_ConfigureHost_when_called_after_it()
    {
        var recorder = RunProbeCommand(builder => builder
                                                  .ConfigureHost(host => host.ConfigureAppConfiguration(configuration =>
                                                                                                            AddProbeKey(configuration, "from-host")))
                                                  .ConfigureAppConfiguration(configuration => AddProbeKey(configuration, "from-configuration")));

        recorder.ExitCode.Should().Be(0);
        recorder.ConfigurationValue.Should().Be("from-configuration", "the source added by the later call is the last one, so it wins");
    }

    [Fact]
    public void ConfigureHost_should_take_precedence_over_ConfigureAppConfiguration_when_called_after_it()
    {
        var recorder = RunProbeCommand(builder => builder
                                                  .ConfigureAppConfiguration(configuration => AddProbeKey(configuration, "from-configuration"))
                                                  .ConfigureHost(host => host.ConfigureAppConfiguration(configuration =>
                                                                                                            AddProbeKey(configuration, "from-host"))));

        recorder.ExitCode.Should().Be(0);
        recorder.ConfigurationValue.Should().Be("from-host", "the source added by the later call is the last one, so it wins");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_override_the_default_appsettings_file()
    {
        // appsettings.json is loaded by default; a source the caller adds must come after it so the caller can override
        // it. The file lives in a private content root rather than the test working directory, so no other test - in
        // this collection or one running in parallel - can see it, and a crashed run cannot leave it behind there.
        var contentRoot = Directory.CreateTempSubdirectory("ploch-appbuilder-").FullName;
        File.WriteAllText(Path.Join(contentRoot, "appsettings.json"),
                          """{ "probe": { "key": "from-appsettings", "other": "from-appsettings" } }""");

        try
        {
            var recorder = RunProbeCommand(builder => builder.ConfigureHost(host => host.UseContentRoot(contentRoot))
                                                             .ConfigureAppConfiguration(configuration =>
                                                                                            AddProbeKey(configuration, "from-configuration")));

            recorder.ExitCode.Should().Be(0);
            recorder.SecondConfigurationValue.Should().Be("from-appsettings", "the default appsettings.json source is still loaded");
            recorder.ConfigurationValue.Should().Be("from-configuration", "sources the caller adds come after the default appsettings.json");
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void ConfigureCommandApp_should_let_command_line_arguments_override_the_appsettings_file()
    {
        // Host.CreateDefaultBuilder layers the command line above appsettings.json. The builder used to add
        // appsettings.json a second time on top of that, silently inverting the precedence (issue #82).
        var contentRoot = Directory.CreateTempSubdirectory("ploch-appbuilder-").FullName;
        File.WriteAllText(Path.Join(contentRoot, "appsettings.json"),
                          """{ "probe": { "key": "from-appsettings", "other": "from-appsettings" } }""");

        try
        {
            var recorder = new ProbeRecorder();
            ProbeCommand.Recorder = recorder;
            using var cancellationTokenSource = new CancellationTokenSource();
            var builder = new AppBuilder(new ConsoleAppInfo("--probe:key=from-command-line") { Name = "Probe App" }, cancellationTokenSource)
                .ConfigureHost(host => host.UseContentRoot(contentRoot));

            recorder.ExitCode = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe")).Run("probe");

            recorder.ExitCode.Should().Be(0);
            recorder.SecondConfigurationValue.Should().Be("from-appsettings", "appsettings.json is still loaded");
            recorder.ConfigurationValue.Should().Be("from-command-line", "the command line outranks appsettings.json");
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void ConfigureCommandApp_should_defer_a_call_made_from_inside_a_host_delegate_to_the_next_build()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        var calledBack = false;
        builder.ConfigureHost(_ =>
                              {
                                  if (calledBack)
                                  {
                                      return;
                                  }

                                  // Re-entrant: records a new operation while the builder is replaying its list.
                                  calledBack = true;
                                  builder.ConfigureServices(services => services.AddSingleton(new Marker { Name = "late" }));
                              });

        var firstRun = new ProbeRecorder();
        ProbeCommand.Recorder = firstRun;
        firstRun.ExitCode = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe")).Run("probe");

        var secondRun = new ProbeRecorder();
        ProbeCommand.Recorder = secondRun;
        secondRun.ExitCode = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe")).Run("probe");

        firstRun.ExitCode.Should().Be(0, "modifying the operation list mid-replay must not break the build in progress");
        firstRun.MarkerNames.Should().BeEmpty("a call recorded during replay is not part of the build already in progress");
        secondRun.ExitCode.Should().Be(0);
        secondRun.MarkerNames.Should().Equal(LateOnly, "the recorded call applies to the next build");
    }

    [Fact]
    public void ConfigureCommandApp_should_apply_every_call_of_every_kind_in_call_order()
    {
        var recorder = RunProbeCommand(builder => builder
                                                  .ConfigureServices(services => services.AddSingleton(new Marker { Name = "services-1" }))
                                                  .ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "host-1" })))
                                                  .ConfigureAppConfiguration(configuration => AddProbeKey(configuration, "configuration-1"))
                                                  .ConfigureServices((_, services) => services.AddSingleton(new Marker { Name = "services-2" }))
                                                  .ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                    services.AddSingleton(new Marker
                                                                                                        { Name = "host-2" })))
                                                  .ConfigureAppConfiguration((_, configuration) =>
                                                                                 configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["probe:other"] = "configuration-2"
                                                                                     })));

        recorder.ExitCode.Should().Be(0);
        recorder.MarkerNames.Should().Equal(InterleavedServiceCalls, "every call is kept and replayed in the order it was made");
        recorder.ConfigurationValue.Should().Be("configuration-1");
        recorder.SecondConfigurationValue.Should().Be("configuration-2");
    }

    [Fact]
    public void ConfigureServices_should_not_be_able_to_replace_the_cancellation_token_source_the_builder_was_created_with()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        // The impostors are created by factories, so the container that would resolve them owns them, rather than being
        // test-scoped instances captured by delegates. If either registration won, resolution would return a fresh
        // source, which is not the one asserted below.
        var recorder = RunProbeCommand(builder => builder.ConfigureServices(services => services.AddSingleton(_ => new CancellationTokenSource()))
                                                         .ConfigureHost(host => host.ConfigureServices(services =>
                                                                                                           services.AddSingleton(_ =>
                                                                                                               new CancellationTokenSource()))),
                                       cancellationTokenSource);

        recorder.ExitCode.Should().Be(0);
        recorder.CancellationTokenSource.Should()
                .BeSameAs(cancellationTokenSource, "the builder registers its own source after every caller registration");
    }

    [Fact]
    public void ConfigureServices_should_run_after_the_registered_services_bundles()
    {
        var recorder = RunProbeCommand(builder => builder.ConfigureServices(services => services.AddSingleton(new Marker { Name = "from-services" }))
                                                         .AddServicesBundle<MarkerServicesBundle>());

        recorder.ExitCode.Should().Be(0);
        recorder.Marker!.Name.Should().Be("from-services", "bundles are the defaults a caller's own registrations override");
    }

    [Fact]
    public void ConfigureServices_should_reject_a_null_delegate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var withoutContext = () => builder.ConfigureServices((Action<IServiceCollection>)null!);
        var withContext = () => builder.ConfigureServices((Action<HostBuilderContext, IServiceCollection>)null!);

        withoutContext.Should()
                      .Throw<ArgumentNullException>("a delegate that is only stored would otherwise fail much later, while the host is being built")
                      .WithParameterName("servicesConfigurator");
        withContext.Should().Throw<ArgumentNullException>().WithParameterName("servicesConfigurator");
    }

    [Fact]
    public void ConfigureAppConfiguration_should_reject_a_null_delegate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var withoutContext = () => builder.ConfigureAppConfiguration((Action<IConfigurationBuilder>)null!);
        var withContext = () => builder.ConfigureAppConfiguration((Action<HostBuilderContext, IConfigurationBuilder>)null!);

        withoutContext.Should().Throw<ArgumentNullException>().WithParameterName("appConfigurationConfigurator");
        withContext.Should().Throw<ArgumentNullException>().WithParameterName("appConfigurationConfigurator");
    }

    [Fact]
    public void ConfigureHost_should_reject_a_null_delegate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var act = () => builder.ConfigureHost(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configureDelegate");
    }

    [Fact]
    public void Dispose_should_release_the_cancellation_token_source_it_created()
    {
        CancellationTokenSource? cancellationTokenSource;

        using (var builder = AppBuilder.Create().WithName("Widget Tool"))
        {
            var recorder = new ProbeRecorder();
            ProbeCommand.Recorder = recorder;
            var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

            // Run inside the block: the source has to stay live for the whole run, so the probe reaches
            // for the very instance the builder published to the application's services.
            executor.Run("probe").Should().Be(0);
            cancellationTokenSource = recorder.CancellationTokenSource;
            cancellationTokenSource.Should().NotBeNull("the probe resolves the source the builder registered");
        }

        var useAfterDispose = () => cancellationTokenSource!.Token;
        useAfterDispose.Should()
                       .Throw<ObjectDisposedException>("Create makes the source itself, so the builder owns it and releases it on Dispose");
    }

    [Fact]
    public void Dispose_should_leave_a_caller_supplied_cancellation_token_source_alone()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        builder.Dispose();

        var useAfterDispose = () => cancellationTokenSource.Token;
        useAfterDispose.Should().NotThrow("a source handed to the constructor belongs to the caller, who may still need it afterwards");
        cancellationTokenSource.IsCancellationRequested.Should().BeFalse("disposing the builder is not a request to cancel");
    }

    [Fact]
    public void Dispose_should_be_safe_to_call_more_than_once()
    {
        var builder = AppBuilder.Create().WithName("Widget Tool");
        builder.Dispose();

        Action disposeAgain = builder.Dispose;

        disposeAgain.Should().NotThrow("Dispose has to be idempotent - a using block around an already-disposed builder is legal");
    }

    [Fact]
    public void ConfigureCommandApp_should_reject_a_disposed_builder()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        builder.Dispose();

        var act = () => builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        act.Should()
           .Throw<ObjectDisposedException>("building after disposal would publish a released cancellation source to the application's services");
    }

    [Fact]
    public void CancelKeyPress_should_cancel_the_token_and_suppress_termination_on_the_first_interrupt()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool");
        var handler = GetInstalledCancelKeyPressHandler(builder);
        var interrupt = NewConsoleCancelEventArgs();

        handler(sender: null, interrupt);

        interrupt.Cancel.Should().BeTrue("the first interrupt is handled cooperatively rather than killing the process");
        GetOwnedCancellationTokenSource(builder)
            .IsCancellationRequested.Should()
            .BeTrue("the interrupt has to reach the token the running command was given");
    }

    [Fact]
    public void CancelKeyPress_should_not_suppress_a_second_interrupt()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool");
        var handler = GetInstalledCancelKeyPressHandler(builder);

        var first = NewConsoleCancelEventArgs();
        handler(sender: null, first);

        var second = NewConsoleCancelEventArgs();
        handler(sender: null, second);

        first.Cancel.Should().BeTrue();
        second.Cancel.Should()
              .BeFalse("the handler is one-shot: a second interrupt takes the default path so a command that ignores "
                       + "its token cannot leave the application unkillable from the keyboard");
    }

    [Fact]
    public void CancelKeyPress_should_report_a_failing_cancellation_callback_rather_than_let_it_escape()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool");
        var handler = GetInstalledCancelKeyPressHandler(builder);

        // Cancel() runs consumer callbacks synchronously and wraps anything they throw in an
        // AggregateException. Unhandled on the CancelKeyPress thread that terminates the process, which is
        // the opposite of the graceful shutdown the interrupt asked for.
        using var registration = GetOwnedCancellationTokenSource(builder)
                                 .Token.Register(() => throw new InvalidOperationException("callback exploded"));
        var interrupt = NewConsoleCancelEventArgs();

        var act = () => handler(sender: null, interrupt);

        act.Should().NotThrow("an exception escaping here would kill the process during a requested shutdown");
        interrupt.Cancel.Should().BeTrue("a consumer's failing callback must not turn a cooperative shutdown into a kill");
        _console.Output.Should().Contain("callback exploded", "the failure is reported rather than silently swallowed");
    }

    [Fact]
    public void CancelKeyPress_should_defer_disposal_a_cancellation_callback_asks_for_until_cancellation_unwinds()
    {
        using var builder = AppBuilder.Create().WithName("Widget Tool");
        var handler = GetInstalledCancelKeyPressHandler(builder);
        var cancellationTokenSource = GetOwnedCancellationTokenSource(builder);
        var callbackRan = false;

        // The re-entrant case: Cancel() runs this synchronously, so Dispose is reached from inside the very
        // call that is still unwinding. Holding a re-entrant lock across Cancel would not prevent the overlap -
        // the nested Dispose would simply re-acquire the lock it already owns.
        using var registration = cancellationTokenSource.Token.Register(() =>
                                                                        {
                                                                            callbackRan = true;
                                                                            builder.Dispose();
                                                                        });

        var interrupt = NewConsoleCancelEventArgs();

        var act = () => handler(sender: null, interrupt);

        act.Should().NotThrow("disposing from a cancellation callback must not tear the source down mid-Cancel");
        callbackRan.Should().BeTrue("the callback has to have run for this to be testing anything");
        interrupt.Cancel.Should().BeTrue("cancellation was still requested, so the interrupt was handled");

        var useAfterDispose = () => cancellationTokenSource.Token;
        useAfterDispose.Should()
                       .Throw<ObjectDisposedException>("the disposal was deferred, not skipped - the cancelling "
                                                       + "thread releases the source once Cancel has unwound");
    }

    [Fact]
    public void CancelKeyPress_should_not_suppress_an_interrupt_that_arrives_after_disposal()
    {
        var builder = AppBuilder.Create().WithName("Widget Tool");
        var handler = GetInstalledCancelKeyPressHandler(builder);
        builder.Dispose();

        var interrupt = NewConsoleCancelEventArgs();
        handler(sender: null, interrupt);

        interrupt.Cancel.Should()
                 .BeFalse("the source is gone, so there is nothing to cancel - suppressing the press as well would "
                          + "leave one that neither stops nor terminates the application");
    }

    /// <summary>Adds an in-memory source supplying <c>probe:key</c>, the value the probe command reports.</summary>
    /// <param name="configuration">The configuration builder to add the source to.</param>
    /// <param name="value">The value the source supplies for <c>probe:key</c>.</param>
    private static void AddProbeKey(IConfigurationBuilder configuration, string value) =>
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["probe:key"] = value });

    /// <summary>
    ///     Builds an application configured by <paramref name="configure" /> and runs a probe command through it.
    ///     The probe reports back through a static slot rather than a registered service, so that the helper's own
    ///     registrations stay out of the way of whatever the test configured — the marker assertions count exactly
    ///     the registrations the test made.
    /// </summary>
    private static ProbeRecorder RunProbeCommand(Action<AppBuilder> configure, CancellationTokenSource? cancellationTokenSource = null)
    {
        // Only the source this helper creates is disposed here. A caller-supplied one belongs to the test that
        // made it and may still be needed after this call returns, so disposing it would be a use-after-dispose
        // waiting to happen.
        using var ownedCancellationTokenSource = cancellationTokenSource is null ? new CancellationTokenSource() : null;

        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource ?? ownedCancellationTokenSource!);
        configure(builder);

        var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        recorder.ExitCode = executor.Run("probe");

        return recorder;
    }

    /// <summary>
    ///     Returns the <c>Console.CancelKeyPress</c> handler <see cref="AppBuilder.Create" /> installed.
    /// </summary>
    /// <remarks>
    ///     Reached through the field rather than the event because <c>Console.CancelKeyPress</c> cannot be raised from
    ///     a test and the handler itself is a local function inside <c>Create</c>. The alternative — spawning a child
    ///     process and sending it a real interrupt — is operating-system specific and flaky under CI, and would test
    ///     the harness as much as the handler. The behaviour asserted here is genuinely observable; only the route to
    ///     it is not.
    /// </remarks>
    private static ConsoleCancelEventHandler GetInstalledCancelKeyPressHandler(AppBuilder builder)
    {
        var field = typeof(AppBuilder).GetField("_cancelKeyPressHandler", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull("the builder keeps the handler so that Dispose can unsubscribe that exact instance");

        return (ConsoleCancelEventHandler)field!.GetValue(builder)!;
    }

    /// <summary>Returns the cancellation source <see cref="AppBuilder.Create" /> made and owns.</summary>
    private static CancellationTokenSource GetOwnedCancellationTokenSource(AppBuilder builder)
    {
        var field = typeof(AppBuilder).GetField("_cancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();

        return (CancellationTokenSource)field!.GetValue(builder)!;
    }

    /// <summary>
    ///     Creates a <see cref="ConsoleCancelEventArgs" />, which has no public constructor — only the runtime raises
    ///     the event normally.
    /// </summary>
    private static ConsoleCancelEventArgs NewConsoleCancelEventArgs() =>
        (ConsoleCancelEventArgs)Activator.CreateInstance(typeof(ConsoleCancelEventArgs),
                                                         BindingFlags.Instance | BindingFlags.NonPublic,
                                                         binder: null,
                                                         [ConsoleSpecialKey.ControlC],
                                                         culture: null)!;

    private sealed class Marker
    {
        public string Name { get; init; } = "marker";
    }

    /// <summary>Captures what the running command could see, so the builder's configuration can be asserted end to end.</summary>
    private sealed class ProbeRecorder
    {
        public int ExitCode { get; set; } = int.MinValue;

        public Marker? Marker { get; set; }

        /// <summary>Every marker the container holds, so a test can tell one registration from two.</summary>
        public List<string> MarkerNames { get; } = [];

        public string? ConfigurationValue { get; set; }

        public string? SecondConfigurationValue { get; set; }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        /// <summary>
        ///     The token Spectre handed to <c>Execute</c>. Distinct from <see cref="CancellationTokenSource" />,
        ///     which only proves the source reached the container: this proves it reached the running command.
        /// </summary>
        public CancellationToken ExecutionToken { get; set; }
    }

    private sealed class ProbeSettings : CommandSettings
    {
    }

    private sealed class ProbeCommand(IConfiguration configuration, CancellationTokenSource cancellationTokenSource, IServiceProvider services)
        : Command<ProbeSettings>
    {
        /// <summary>Set by the test before the run; safe because this class runs in a non-parallel collection.</summary>
        public static ProbeRecorder? Recorder { get; set; }

        public override int Execute(CommandContext context, ProbeSettings settings, CancellationToken cancellationToken)
        {
            var recorder = Recorder ?? throw new InvalidOperationException("The probe recorder was not set before the run.");
            recorder.ConfigurationValue = configuration["probe:key"];
            recorder.SecondConfigurationValue = configuration["probe:other"];
            recorder.CancellationTokenSource = cancellationTokenSource;
            recorder.ExecutionToken = cancellationToken;
            recorder.MarkerNames.AddRange(services.GetServices<Marker>().Select(marker => marker.Name));
            recorder.Marker = services.GetService<Marker>();

            return 0;
        }
    }

    private sealed class MarkerServicesBundle : ServicesBundle
    {
        public override void DoConfigure() => Services.AddSingleton(new Marker());
    }
}
