using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Ploch.Common.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

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

    /// <summary>Stands in for standard output: the console Spectre and <c>IOutput</c> render to.</summary>
    private readonly RecordingConsole _console = new();

    /// <summary>Stands in for standard error: the console the builder writes the banner and the "Executing command" line to.</summary>
    private readonly RecordingConsole _errorConsole = new();

    public AppBuilderTests()
    {
        AnsiConsole.Console = _console.Console;
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());
    }

    public void Dispose()
    {
        ProbeCommand.Recorder = null;
        ProbeHostedService.Reset();
        RecordingExceptionHandler.Reset();
        ThrowingPlainCommand.ExceptionFactory = null;
        AnsiConsole.Console = _originalConsole;
        EnvironmentSettings.Reset();
        _console.Dispose();
        _errorConsole.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Counts how many times <paramref name="text" /> appears in the recorded standard output.</summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The number of non-overlapping occurrences.</returns>
    private int OccurrencesOf(string text) => _console.Output.Split(text).Length - 1;

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
    public void ConfigureCommandApp_should_print_the_start_up_banner_to_standard_error()
    {
        var recorder = RunProbeCommand(builder => builder.WithName("Widget Tool").WithVersion(new Version(4, 5)).WithDescription("Widget things"));

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Widget Tool 4.5").And.Contain("Widget things");
        _errorConsole.Output.Split(Environment.NewLine).Count(line => line.Trim().Length > 0).Should().BeGreaterThan(4, "the FIGlet art spans several lines above the name and description lines");
        _console.Output.Should().NotContain("Widget Tool").And.NotContain("Widget things", "the banner must stay out of the output a consumer pipes (PLO-783)");
    }

    [Fact]
    public void ConfigureCommandApp_should_not_print_the_banner_when_it_is_switched_off()
    {
        var recorder = RunProbeCommand(builder => builder.WithName("Widget Tool").WithDescription("Widget things").ShowBanner(false));

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().BeEmpty("the switch silences the banner and the announcement alike");
        _console.Output.Should().NotContain("Widget Tool");
    }

    [Fact]
    public void ShowBanner_should_set_the_switch_on_the_app_info()
    {
        var appInfo = new ConsoleAppInfo();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(appInfo, cancellationTokenSource, _errorConsole.Console);

        builder.ShowBanner(false).Should().BeSameAs(builder, "the fluent methods chain");

        appInfo.ShouldShowBanner.Should().BeFalse();
        builder.ShowBanner(true).Should().BeSameAs(builder);
        appInfo.ShouldShowBanner.Should().BeTrue();
    }

    /// <summary>
    ///     The announcement names the command the user typed and goes to standard error, so standard output carries
    ///     only what the command itself renders — here nothing, since the probe writes nothing (PLO-783).
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_announce_the_command_by_name_on_standard_error_and_leave_standard_output_to_the_command()
    {
        var recorder = RunProbeCommand(_ => { });

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Executing command probe", "the line names the command as invoked");
        _errorConsole.Output.Should().NotContain(nameof(ProbeSettings), "the settings type is an implementation detail");
        _console.Output.Should().BeEmpty("the probe writes nothing, and neither may the framework");
    }

    /// <summary>Spectre's <c>CommandContext.Name</c> is the leaf name, so a branched command is announced as its leaf.</summary>
    [Fact]
    public void ConfigureCommandApp_should_announce_a_branched_command_by_its_leaf_name()
    {
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        recorder.ExitCode = builder.ConfigureCommandApp(configurator => configurator.AddBranch("user", user => user.AddCommand<ProbeCommand>("add")))
                                   .Run("user", "add");

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Executing command add").And.NotContain("Executing command user");
    }

    /// <summary>
    ///     One switch for everything the library says on standard error: a tool whose standard error is captured as
    ///     well wants the announcement gone with the banner.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_not_announce_the_command_when_the_banner_is_switched_off()
    {
        var recorder = RunProbeCommand(builder => builder.ShowBanner(false));

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().BeEmpty("the switch silences the announcement as well as the banner");
    }

    /// <summary>
    ///     Spectre resolves every registered <c>ICommandInterceptor</c>, so the library's announcement adds to an
    ///     interceptor the consumer registers rather than replacing it, whichever way the consumer registered it.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_run_the_announcement_before_an_interceptor_registered_through_ConfigureServices()
    {
        RecordingInterceptor.Reset();
        RecordingInterceptor.ErrorOutputProbe = () => _errorConsole.Output;

        var recorder = RunProbeCommand(builder => builder.ConfigureServices(services => services.AddSingleton<ICommandInterceptor, RecordingInterceptor>()));

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Executing command probe");
        RecordingInterceptor.InterceptCalls.Should().Be(1, "the consumer's interceptor runs once alongside the library's");
        RecordingInterceptor.ResultCalls.Should().Be(1);
        RecordingInterceptor.AnnouncementSeenFirst.Should().BeTrue("the library registers its interceptor with the defaults, ahead of the caller's delegates, and Spectre runs them in registration order");
    }

    [Fact]
    public void ConfigureCommandApp_should_run_the_announcement_before_an_interceptor_registered_by_a_services_bundle()
    {
        RecordingInterceptor.Reset();
        RecordingInterceptor.ErrorOutputProbe = () => _errorConsole.Output;

        var recorder = RunProbeCommand(builder => builder.AddServicesBundle<InterceptorServicesBundle>());

        recorder.ExitCode.Should().Be(0);
        RecordingInterceptor.InterceptCalls.Should().Be(1);
        RecordingInterceptor.AnnouncementSeenFirst.Should().BeTrue("the library's interceptor is registered before the bundles are initialised");
    }

    /// <summary>
    ///     A branch's default command runs when the branch was named without a command; Spectre names it with an
    ///     internal placeholder, which the announcement must not expose. Running a real default command pins the
    ///     placeholder Spectre actually uses. (A root default command needs <c>CommandApp.SetDefaultCommand</c>, which
    ///     the builder does not expose, so the branch form is the one a consumer can reach.)
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_announce_a_default_command_as_the_default_command()
    {
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        recorder.ExitCode = builder.ConfigureCommandApp(configurator => configurator.AddBranch("user", user => user.SetDefaultCommand<ProbeCommand>()))
                                   .Run("user");

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Executing the default command").And.NotContain("__default_command");
    }

    [Fact]
    public void ConfigureCommandApp_should_run_the_announcement_before_an_interceptor_set_through_the_configurator()
    {
        RecordingInterceptor.Reset();
        RecordingInterceptor.ErrorOutputProbe = () => _errorConsole.Output;
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        recorder.ExitCode = builder.ConfigureCommandApp(configurator =>
                                   {
                                       configurator.SetInterceptor(new RecordingInterceptor());
                                       configurator.AddCommand<ProbeCommand>("probe");
                                   })
                                   .Run("probe");

        recorder.ExitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Executing command probe");
        RecordingInterceptor.InterceptCalls.Should().Be(1, "SetInterceptor registers through the same registrar, so both interceptors run");
        RecordingInterceptor.AnnouncementSeenFirst.Should().BeTrue("Spectre's registrar registers the consumer's interceptor after the library's");
    }

    /// <summary>
    ///     Help is handled by Spectre before any command is resolved, so the banner is the only thing the application
    ///     adds — on standard error — and there is no command to announce.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_print_the_banner_but_not_announce_a_command_for_help()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ProbeCommand>("probe");
                              })
                              .Run("--help");

        exitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Probe App").And.NotContain("Executing command");
        _console.Output.Should().Contain("USAGE", "Spectre renders help on its own console").And.NotContain("Probe App");
    }

    [Fact]
    public void ConfigureCommandApp_should_print_the_banner_but_not_announce_a_command_for_version()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);

                                  // Spectre answers --version only when an application version is configured; without
                                  // one the switch is an unknown option and the run fails with -1.
                                  configurator.SetApplicationVersion("1.2.3");
                                  configurator.AddCommand<ProbeCommand>("probe");
                              })
                              .Run("--version");

        exitCode.Should().Be(0);
        _errorConsole.Output.Should().Contain("Probe App").And.NotContain("Executing command", "no command is resolved for the version switch");
        _console.Output.Should().Contain("1.2.3", "Spectre renders the version on its own console").And.NotContain("Probe App").And.NotContain("Executing command");
    }

    /// <summary>
    ///     The announcement is written before the command runs, so a failing standard error stream stops the run
    ///     before the command: Spectre hands the exception to the application's handler as it would any failure
    ///     ahead of execution, which passes it to the registered <c>IExceptionHandler</c>, whose return value is the
    ///     exit code, rather than through the command's own catch (PLO-783, PLO-782).
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_fail_the_run_before_the_command_when_the_announcement_cannot_be_written()
    {
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        using var cancellationTokenSource = new CancellationTokenSource();
        var errorConsole = new ThrowingConsole(_errorConsole.Console);
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, errorConsole)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();

        var executor = builder.ConfigureCommandApp(configurator =>
                                                   {
                                                       configurator.ConfigureConsole(_console.Console);
                                                       configurator.AddCommand<ProbeCommand>("probe");
                                                   });

        // The banner has been written by now; from here on standard error is "closed", so the announcement fails.
        errorConsole.FailWrites = true;
        recorder.ExitCode = executor.Run("probe");

        recorder.ExitCode.Should().Be(RecordingExceptionHandler.ReturnedExitCode, "a run that could not announce its command did not complete, and the registered handler decided the exit code");
        RecordingExceptionHandler.Exceptions.Should().ContainSingle("the registered handler was asked to handle the announcement failure, once");
        recorder.ExecutionToken.Should().Be(CancellationToken.None, "the command never ran, so it recorded nothing");
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

    /// <summary>
    ///     Pins the decision recorded in PLO-780: the host exists for dependency injection, configuration and logging
    ///     and is never started, so a hosted service a consumer registers does not run. Resolving the service from the
    ///     container proves the registration reached the host; the counters prove nothing started or stopped it.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_not_start_the_host_so_a_registered_hosted_service_never_runs()
    {
        // Dispose resets the counters after every test; resetting here as well keeps an aborted earlier run from
        // leaking a count into this one.
        ProbeHostedService.Reset();

        var recorder = RunProbeCommand(builder => builder.ConfigureServices(services => services.AddHostedService<ProbeHostedService>()));

        recorder.ExitCode.Should().Be(0);
        recorder.HostedServiceTypes
                .Should()
                .ContainSingle(type => type == typeof(ProbeHostedService),
                               "the registration must reach the container the command resolves from, or this test proves nothing");
        ProbeHostedService.StartCalls.Should().Be(0, "a CLI command is the unit of work; the host is built, never started (PLO-785 tracks starting it)");
        ProbeHostedService.StopCalls.Should().Be(0, "a host that was never started is not stopped either");
        ProbeHostedService.DisposeCalls.Should().Be(1, "the host, and with it the singleton it constructed, is disposed when the run ends");
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
            var builder = new AppBuilder(new ConsoleAppInfo("--probe:key=from-command-line") { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console)
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
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);
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
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

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
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        var withoutContext = () => builder.ConfigureAppConfiguration((Action<IConfigurationBuilder>)null!);
        var withContext = () => builder.ConfigureAppConfiguration((Action<HostBuilderContext, IConfigurationBuilder>)null!);

        withoutContext.Should().Throw<ArgumentNullException>().WithParameterName("appConfigurationConfigurator");
        withContext.Should().Throw<ArgumentNullException>().WithParameterName("appConfigurationConfigurator");
    }

    [Fact]
    public void ConfigureHost_should_reject_a_null_delegate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

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
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

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

    /// <summary>
    ///     Pins the exit code the documentation gives for settings that fail their own <c>Validate</c>: Spectre.Console.Cli
    ///     constructs the command, calls its <c>Validate</c> override - which hands the settings to the registered
    ///     validator, and the default one asks the settings themselves - and throws on a failure before <c>Execute</c>.
    ///     The handler <see cref="AppBuilder.ConfigureCommandApp" /> installs renders Spectre's message and turns the
    ///     failure into <see cref="ExitCode.InvalidInput" /> (PLO-782); before it, Spectre returned its own <c>-1</c>.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_the_settings_reject_themselves()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RejectingCommand.Executed = false;
        RecordingExceptionHandler.Reset();

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  // Spectre.Console.Cli renders its errors through a process-wide console it captures the first time
                                  // it needs one. Handing it this test's console keeps the rendering out of a console an
                                  // earlier test has already disposed.
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<RejectingCommand>("reject");
                              })
                              .Run("reject");

        exitCode.Should().Be((int)ExitCode.InvalidInput, "a failed Validate is an input error, whatever Spectre's own code for it is");
        _console.Output.Should().Contain("Error: rejected by Validate [brackets kept]", "Spectre's own rendering is kept, with the message escaped rather than parsed as markup");
        _console.Output.Should().NotContain("Error: rejected by Validate [brackets kept]" + Environment.NewLine + "Error:", "the message is rendered once");
        RejectingCommand.Executed.Should().BeFalse("a command whose settings fail validation never runs");
        RecordingExceptionHandler.Exceptions.Should().BeEmpty("a validation failure is not routed through the exception handler");
        _errorConsole.Output.Should().NotContain("Executing command", "the settings are rejected while binding, before any interceptor runs (PLO-783)");
    }

    /// <summary>
    ///     The same exit code when the settings are fine but the registered
    ///     <see cref="ICommandSettingsValidator{TSettings}" /> rejects them. This is the path through the base class's
    ///     <c>Validate</c> override, which Spectre calls after binding and before <c>Execute</c>; the direct
    ///     <c>Execute</c> call that unit tests make skips it, as the Getting Started guide says.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_the_registered_validator_rejects_the_settings()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console)
            .ConfigureServices(services => services.AddSingleton<ICommandSettingsValidator<AcceptingSettings>, RejectingValidator>());
        AcceptingCommand.Executed = false;
        RejectingValidator.Calls = 0;

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("accept");

        exitCode.Should().Be((int)ExitCode.InvalidInput, "a validator failure is reported through the command's Validate override and is an input error");
        RejectingValidator.Calls.Should().Be(1, "Spectre calls the command's Validate override exactly once, and it delegates to the registered validator");
        _console.Output.Should().Contain("rejected by the validator");
        AcceptingCommand.Executed.Should().BeFalse("a command whose validator rejects the settings never runs");
        _errorConsole.Output.Should().Contain("Executing command accept", "Spectre runs the interceptors after binding and before the command's Validate, so the announcement precedes a validator rejection (PLO-783)");
    }

    /// <summary>
    ///     An unknown option is a <c>CommandParseException</c>, which Spectre raises before any host exists and hands to
    ///     the handler with no type resolver. Its pretty rendering - the message and the offending token - is kept.
    ///     Strict parsing is what makes an unknown option an error; under Spectre's default relaxed parsing it is
    ///     collected into the remaining arguments and the command runs.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_and_render_spectres_message_when_an_option_is_unknown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        AcceptingCommand.Executed = false;
        RecordingExceptionHandler.Reset();

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.UseStrictParsing();
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("accept", "--no-such-option");

        exitCode.Should().Be((int)ExitCode.InvalidInput);
        _console.Output.Should().Contain("Unknown option 'no-such-option'", "Spectre's parse error is still rendered");
        AcceptingCommand.Executed.Should().BeFalse();
        RecordingExceptionHandler.Exceptions.Should().BeEmpty("a parse failure is not routed through the exception handler");
    }

    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_the_command_is_unknown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("no-such-command");

        exitCode.Should().Be((int)ExitCode.InvalidInput);
        _console.Output.Should().Contain("Unknown command 'no-such-command'");
    }

    /// <summary>
    ///     A missing required argument is a <c>CommandRuntimeException</c> raised while binding, after the host exists,
    ///     so the handler receives a type resolver and must still classify it as input rather than ask the exception
    ///     handler.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_a_required_argument_is_missing()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ArgumentCommand>("count");
                              })
                              .Run("count");

        exitCode.Should().Be((int)ExitCode.InvalidInput);
        _console.Output.Should().Contain("Command 'count' is missing required argument 'number'");
        RecordingExceptionHandler.Exceptions.Should().BeEmpty();
    }

    /// <summary>
    ///     A value Spectre cannot convert carries the converter's exception as its inner exception. Spectre renders only
    ///     a pretty inner exception, and a <see cref="FormatException" /> has none, so its text must not appear.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_a_value_cannot_be_converted()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ArgumentCommand>("count");
                              })
                              .Run("count", "abc");

        exitCode.Should().Be((int)ExitCode.InvalidInput);
        _console.Output.Should().Contain("Failed to convert 'abc'");
        _console.Output.Should().NotContain("correct format", "the FormatException behind the conversion failure has no pretty rendering and stays out of the output");
    }

    /// <summary>
    ///     An exception that escapes a plain Spectre command - one that does not derive from the framework base classes,
    ///     which catch their own - reaches the installed handler with the host's resolver and goes to the registered
    ///     <see cref="IExceptionHandler" />, exactly once, whose return value is the exit code.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_route_an_exception_from_a_plain_command_to_the_registered_exception_handler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("plain command failed");

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be(RecordingExceptionHandler.ReturnedExitCode);
        RecordingExceptionHandler.Exceptions.Should().ContainSingle().Which.Message.Should().Be("plain command failed");
    }

    /// <summary>
    ///     A handler that throws is reported once and the run exits with <see cref="ExitCode.Error" />, instead of the
    ///     handler being called a second time for its own exception.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_report_a_throwing_exception_handler_and_exit_with_Error()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        RecordingExceptionHandler.Throws = true;
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("plain command failed");

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Error);
        RecordingExceptionHandler.Exceptions.Should().ContainSingle("the handler is not asked to handle the exception it threw itself");
        OccurrencesOf("Error: plain command failed").Should().Be(1, "the failure the handler was asked to report is not lost, and rendered once");
        OccurrencesOf("Error: the handler failed").Should().Be(1, "the handler's failure is rendered once");
    }

    /// <summary>
    ///     The base classes call the handler themselves. One that throws there used to escape to Spectre; it now
    ///     surfaces as <see cref="ExceptionHandlerFailedException" />, which the installed handler reports without
    ///     calling the throwing handler a second time, for the synchronous and the asynchronous base class alike.
    /// </summary>
    /// <param name="command">The registered name of the command to run: the synchronous or the asynchronous one.</param>
    [Theory]
    [InlineData("throw-sync")]
    [InlineData("throw-async")]
    public void ConfigureCommandApp_should_report_a_throwing_exception_handler_once_for_a_base_class_command(string command)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        RecordingExceptionHandler.Throws = true;

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingAppCommand>("throw-sync");
                                  configurator.AddCommand<ThrowingAsyncAppCommand>("throw-async");
                              })
                              .Run(command);

        exitCode.Should().Be((int)ExitCode.Error);
        RecordingExceptionHandler.Exceptions.Should().ContainSingle().Which.Message.Should().Be("base class command failed");
        OccurrencesOf("Error: base class command failed").Should().Be(1, "the failure the handler was asked to report is not lost, and rendered once");
        OccurrencesOf("Error: the handler failed").Should().Be(1, "the handler's failure is rendered once");
    }

    /// <summary>
    ///     A registered handler whose construction fails is a configuration defect, not an absent handler: both the
    ///     command's exception and the construction failure are reported, and the run exits with
    ///     <see cref="ExitCode.Error" />.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_report_both_failures_when_the_exception_handler_cannot_be_constructed()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler>(_ => throw new InvalidOperationException("the handler factory failed")));
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("plain command failed");

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Error);
        OccurrencesOf("Error: plain command failed").Should().Be(1);
        OccurrencesOf("Error: the handler factory failed").Should().Be(1);
    }

    /// <summary>
    ///     A base-class command constructor-injects the handler, so a handler that cannot be constructed stops the
    ///     command being constructed; Spectre reports that as <c>CommandRuntimeException</c>, which exits 2 under the
    ///     documented mapping, unlike the plain-command case where the handler is first resolved to handle an exception.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_a_base_class_command_cannot_get_its_exception_handler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler>(_ => throw new InvalidOperationException("the handler factory failed")));

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingAppCommand>("throw-sync");
                              })
                              .Run("throw-sync");

        exitCode.Should().Be((int)ExitCode.InvalidInput, "Spectre wraps the construction failure in the exception type the mapping sends to 2");
        _console.Output.Should().Contain("Could not resolve type");
    }

    /// <summary>
    ///     The wrapper's public constructors allow an instance with no inner exception. One thrown by a plain command,
    ///     and one thrown while the host is built, are reported themselves rather than dereferencing the missing inner
    ///     exception.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_report_a_wrapper_without_an_inner_exception_itself()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        ThrowingPlainCommand.ExceptionFactory = _ => new ExceptionHandlerFailedException("a bare wrapper from a command");
        using var hostCancellationTokenSource = new CancellationTokenSource();
        var hostBuilder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, hostCancellationTokenSource)
            .ConfigureServices(_ => throw new ExceptionHandlerFailedException("a bare wrapper from the host"));

        var commandExitCode = builder.ConfigureCommandApp(configurator =>
                                     {
                                         configurator.ConfigureConsole(_console.Console);
                                         configurator.AddCommand<ThrowingPlainCommand>("throw");
                                     })
                                     .Run("throw");
        var hostExitCode = hostBuilder.ConfigureCommandApp(configurator =>
                                      {
                                          configurator.ConfigureConsole(_console.Console);
                                          configurator.AddCommand<AcceptingCommand>("accept");
                                      })
                                      .Run("accept");

        commandExitCode.Should().Be((int)ExitCode.Error);
        hostExitCode.Should().Be((int)ExitCode.Error);
        _console.Output.Should().Contain("Error: a bare wrapper from a command").And.Contain("Error: a bare wrapper from the host");
    }

    /// <summary>
    ///     Spectre raises <c>CommandRuntimeException</c> for a command whose dependencies the container cannot resolve,
    ///     which is not an input error but shares the type with the ones that are; the documented mapping sends it to
    ///     <see cref="ExitCode.InvalidInput" /> as well.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_InvalidInput_when_a_command_dependency_cannot_be_resolved()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<UnresolvableCommand>("unresolvable");
                              })
                              .Run("unresolvable");

        exitCode.Should().Be((int)ExitCode.InvalidInput, "the mapping is by exception type, and Spectre uses the same type for this configuration defect");
        _console.Output.Should().Contain("Could not resolve type");
    }

    /// <summary>
    ///     A <c>CommandRuntimeException</c> that reaches Spectre before any host exists cannot be a binding or
    ///     validation failure, which happen after it: here a host delegate rethrows one captured from an earlier run.
    ///     It is rendered and exits with <see cref="ExitCode.Error" />, not <see cref="ExitCode.InvalidInput" />.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_Error_for_a_runtime_exception_raised_before_the_host_exists()
    {
        using var captureCancellationTokenSource = new CancellationTokenSource();
        var captureBuilder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, captureCancellationTokenSource);
        var captureExecutor = captureBuilder.ConfigureCommandApp(configurator =>
        {
            configurator.ConfigureConsole(_console.Console);
            configurator.PropagateExceptions();
            configurator.AddCommand<ArgumentCommand>("count");
        });
        var captured = Record.Exception(() => captureExecutor.Run("count"));
        captured.Should().BeOfType<CommandRuntimeException>("a missing required argument is a binding failure");
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(_ => throw captured!);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("accept");

        exitCode.Should().Be((int)ExitCode.Error, "no host, so no resolver: the failure cannot be a binding or validation failure");
        _console.Output.Should().Contain("missing required argument 'number'");
    }

    /// <summary>
    ///     Spectre renders a pretty inner exception after the outer message, and so does the installed handler. The
    ///     inner one is a real parse exception captured from Spectre, since its constructors are not public.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_render_a_pretty_inner_exception_once_after_the_outer_message()
    {
        using var captureCancellationTokenSource = new CancellationTokenSource();
        var captureBuilder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, captureCancellationTokenSource);
        var captureExecutor = captureBuilder.ConfigureCommandApp(configurator =>
        {
            configurator.ConfigureConsole(_console.Console);
            configurator.PropagateExceptions();
            configurator.AddCommand<AcceptingCommand>("accept");
        });
        var captured = Record.Exception(() => captureExecutor.Run("no-such-command"));
        captured.Should().BeOfType<CommandParseException>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.RemoveAll<IExceptionHandler>());
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("outer failure", captured);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Error);
        _console.Output.Should().Contain("Error: outer failure");
        _console.Output.Split("Unknown command 'no-such-command'").Should().HaveCount(2, "the inner parse error is rendered exactly once, after the outer message");
        _console.Output.IndexOf("Error: outer failure", StringComparison.Ordinal).Should().BeLessThan(_console.Output.IndexOf("Unknown command", StringComparison.Ordinal));
    }

    /// <summary>Spectre's early returns are untouched: a branch invoked without a sub-command shows help and returns 1.</summary>
    [Fact]
    public void ConfigureCommandApp_should_still_return_one_with_help_for_a_branch_without_a_sub_command()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddBranch("group", group => group.AddCommand<AcceptingCommand>("accept"));
                              })
                              .Run("group");

        exitCode.Should().Be(1, "Spectre returns 1 itself after showing help, without raising an exception");
        _console.Output.Should().Contain("USAGE");
    }

    /// <summary>
    ///     With no <see cref="IExceptionHandler" /> registered the resolver has nothing to hand back - Spectre's adapter
    ///     turns that into a <c>CommandRuntimeException</c>, which must not be mistaken for an input error. The original
    ///     exception is rendered and the run exits with <see cref="ExitCode.Error" />.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_render_the_exception_and_exit_with_Error_when_no_exception_handler_is_registered()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.RemoveAll<IExceptionHandler>());
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("plain command failed");

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Error);
        _console.Output.Should().Contain("Error: plain command failed");
    }

    /// <summary>
    ///     A failure while the host is being built happens before any resolver exists, so there is no handler to ask.
    ///     The exception is rendered and the run exits with <see cref="ExitCode.Error" />; Spectre's own code was
    ///     <c>-1</c>.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_render_a_host_build_failure_and_exit_with_Error()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(_ => throw new InvalidOperationException("the host could not be built"));

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("accept");

        exitCode.Should().Be((int)ExitCode.Error);
        _console.Output.Should().Contain("Error: the host could not be built");
    }

    /// <summary>
    ///     Cancellation that was requested through the application's token is the configured cancellation exit code
    ///     wherever it surfaces, here from a plain command that Spectre's own handling would otherwise have passed to the
    ///     exception handler; cancellation nobody requested is a fault and goes to the handler (PLO-756).
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_the_cancellation_exit_code_when_a_plain_command_reports_requested_cancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        ThrowingPlainCommand.ExceptionFactory = source =>
        {
            source.Cancel();

            return new OperationCanceledException(source.Token);
        };

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Cancelled, "Spectre's default cancellation exit code is 130, the same value as ExitCode.Cancelled");
        RecordingExceptionHandler.Exceptions.Should().BeEmpty("requested cancellation is an outcome, not a fault");
    }

    /// <summary>
    ///     Spectre wraps a failure to construct a command in a <c>CommandRuntimeException</c>. A constructor that
    ///     observed the requested cancellation and threw is still a requested cancellation, not an input error, so the
    ///     handler looks through the wrapper.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_the_cancellation_exit_code_when_a_command_constructor_reports_requested_cancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<CancellingConstructorCommand>("cancel");
                              })
                              .Run("cancel");

        exitCode.Should().Be((int)ExitCode.Cancelled, "the OperationCanceledException sits inside Spectre's CommandRuntimeException wrapper");
    }

    /// <summary>
    ///     An <see cref="OperationCanceledException" /> that carries a token still live is not the application's
    ///     cancellation even when the application's token happens to be cancelled by then: it is a fault for the handler.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_route_a_cancellation_for_a_live_token_to_the_exception_handler_even_after_the_application_was_cancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var unrelatedTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        var unrelatedToken = unrelatedTokenSource.Token;
        ThrowingPlainCommand.ExceptionFactory = source =>
        {
            source.Cancel();

            return new OperationCanceledException("a live token elsewhere", unrelatedToken);
        };

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be(RecordingExceptionHandler.ReturnedExitCode);
        RecordingExceptionHandler.Exceptions.Should().ContainSingle().Which.Should().BeOfType<OperationCanceledException>();
    }

    /// <summary>A command that waited on tasks may surface the cancellation inside an <see cref="AggregateException" />.</summary>
    [Fact]
    public void ConfigureCommandApp_should_exit_with_the_cancellation_exit_code_when_the_cancellation_is_inside_an_aggregate_exception()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        ThrowingPlainCommand.ExceptionFactory = source =>
        {
            source.Cancel();

            return new AggregateException(new InvalidOperationException("a sibling task failed"), new OperationCanceledException(source.Token));
        };

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be((int)ExitCode.Cancelled, "the cancellation is the second inner exception, which a walk down InnerException alone would miss");
    }

    [Fact]
    public void ConfigureCommandApp_should_honour_a_configured_cancellation_exit_code()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        ThrowingPlainCommand.ExceptionFactory = source =>
        {
            source.Cancel();

            return new OperationCanceledException(source.Token);
        };

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.CancellationExitCode(7);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be(7);
    }

    [Fact]
    public void ConfigureCommandApp_should_route_unrequested_cancellation_from_a_plain_command_to_the_exception_handler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource)
            .ConfigureServices(services => services.AddSingleton<IExceptionHandler, RecordingExceptionHandler>());
        RecordingExceptionHandler.Reset();
        ThrowingPlainCommand.ExceptionFactory = _ => new OperationCanceledException("an inner timeout");

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<ThrowingPlainCommand>("throw");
                              })
                              .Run("throw");

        exitCode.Should().Be(RecordingExceptionHandler.ReturnedExitCode);
        RecordingExceptionHandler.Exceptions.Should().ContainSingle().Which.Should().BeOfType<OperationCanceledException>();
    }

    /// <summary>
    ///     The installed handler is set before the caller's delegate runs, so a <c>SetExceptionHandler</c> call in the
    ///     delegate replaces it, at both of Spectre's catch points: parsing (no resolver) and execution (resolver).
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_let_the_configurator_replace_the_installed_exception_handler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);
        ThrowingPlainCommand.ExceptionFactory = _ => new InvalidOperationException("plain command failed");

        var parseExitCode = builder.ConfigureCommandApp(configurator =>
                                   {
                                       configurator.ConfigureConsole(_console.Console);
                                       configurator.SetExceptionHandler((_, _) => 42);
                                       configurator.AddCommand<ThrowingPlainCommand>("throw");
                                   })
                                   .Run("no-such-command");
        var executionExitCode = builder.ConfigureCommandApp(configurator =>
                                       {
                                           configurator.ConfigureConsole(_console.Console);
                                           configurator.SetExceptionHandler((_, _) => 42);
                                           configurator.AddCommand<ThrowingPlainCommand>("throw");
                                       })
                                       .Run("throw");

        parseExitCode.Should().Be(42, "the caller's handler decides the exit code of a parse failure");
        executionExitCode.Should().Be(42, "the caller's handler decides the exit code of an execution failure");
    }

    [Fact]
    public void ConfigureCommandApp_should_let_the_configurator_propagate_exceptions_instead()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var executor = builder.ConfigureCommandApp(configurator =>
        {
            configurator.ConfigureConsole(_console.Console);
            configurator.PropagateExceptions();
            configurator.AddCommand<AcceptingCommand>("accept");
        });
        var act = () => executor.Run("no-such-command");

        act.Should().Throw<CommandParseException>("PropagateExceptions switches Spectre's handling off, installed handler included");
    }

    /// <summary>Spectre's early returns are untouched: help is not an error.</summary>
    [Fact]
    public void ConfigureCommandApp_should_still_return_zero_for_help()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource);

        var exitCode = builder.ConfigureCommandApp(configurator =>
                              {
                                  configurator.ConfigureConsole(_console.Console);
                                  configurator.AddCommand<AcceptingCommand>("accept");
                              })
                              .Run("accept", "--help");

        exitCode.Should().Be(0);
        _console.Output.Should().Contain("USAGE");
    }

    [Fact]
    public void Constructor_should_reject_a_null_app_info()
    {
        var act = () =>
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            return new AppBuilder(null!, cancellationTokenSource);
        };

        act.Should().Throw<ArgumentNullException>("a null stored now would only surface as a NullReferenceException inside ConfigureCommandApp")
           .WithParameterName("appInfo");
    }

    [Fact]
    public void Constructor_should_reject_a_null_cancellation_token_source()
    {
        var act = () => new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cancellationTokenSource");
    }

    [Fact]
    public void ConfigureCommandApp_should_reject_a_null_configurator()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);

        var act = () => builder.ConfigureCommandApp(null!);

        act.Should().Throw<ArgumentNullException>("a null delegate would otherwise fail as a NullReferenceException inside Spectre")
           .WithParameterName("configurator");
    }

    /// <summary>Pins the precedence between the two guards, so it is a decision rather than an accident.</summary>
    [Fact]
    public void ConfigureCommandApp_should_reject_a_null_configurator_before_checking_for_disposal()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);
        builder.Dispose();

        var act = () => builder.ConfigureCommandApp(null!);

        act.Should().Throw<ArgumentNullException>("arguments are validated before object state, as in the rest of .NET").WithParameterName("configurator");
    }

    /// <summary>
    ///     PLO-756: the executor is single-use. The host behind the type registrar is built on the first run and
    ///     <see cref="IHostBuilder.Build" /> cannot be called twice, so a second run could only fail; it now fails
    ///     before reaching Spectre, with a message naming the recovery path, which this test also exercises.
    /// </summary>
    [Fact]
    public void ConfigureCommandApp_should_return_an_executor_that_runs_once()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);
        ProbeCommand.Recorder = new ProbeRecorder();
        var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));
        executor.Run("probe").Should().Be(0);

        var secondRun = () => executor.Run("probe");

        secondRun.Should()
                 .Throw<InvalidOperationException>("the host behind the executor has already been built")
                 .WithMessage("*ConfigureCommandApp*", "the message has to tell the caller how to run again");

        var rebuilt = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));
        rebuilt.Run("probe").Should().Be(0, "a fresh ConfigureCommandApp call on the same builder is the documented way to run again");
    }

    [Fact]
    public void ConfigureCommandApp_should_reject_a_disposed_builder()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);
        builder.Dispose();

        var act = () => builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        act.Should()
           .Throw<ObjectDisposedException>("building after disposal would publish a released cancellation source to the application's services");
    }

    [Fact]
    public void ConfigureCommandApp_should_reject_a_builder_whose_cancellation_source_is_already_cancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource, _errorConsole.Console);
        cancellationTokenSource.Cancel();

        var act = () => builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        act.Should()
           .Throw<InvalidOperationException>("cancellation is permanent, so an executor built now would hand every command an already-cancelled token")
           .WithMessage("*already been cancelled*");
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

    /// <summary>
    ///     Builds an application configured by <paramref name="configure" /> and runs a probe command through it.
    ///     The probe reports back through a static slot rather than a registered service, so that the helper's own
    ///     registrations stay out of the way of whatever the test configured — the marker assertions count exactly
    ///     the registrations the test made.
    /// </summary>
    private ProbeRecorder RunProbeCommand(Action<AppBuilder> configure, CancellationTokenSource? cancellationTokenSource = null)
    {
        // Only the source this helper creates is disposed here. A caller-supplied one belongs to the test that
        // made it and may still be needed after this call returns, so disposing it would be a use-after-dispose
        // waiting to happen.
        using var ownedCancellationTokenSource = cancellationTokenSource is null ? new CancellationTokenSource() : null;

        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource ?? ownedCancellationTokenSource!, _errorConsole.Console);
        configure(builder);

        var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        recorder.ExitCode = executor.Run("probe");

        return recorder;
    }

    private sealed class Marker
    {
        public string Name { get; init; } = "marker";
    }

    /// <summary>A consumer bundle that registers an interceptor, the way a library of commands would.</summary>
    private sealed class InterceptorServicesBundle : ServicesBundle
    {
        public override void DoConfigure() => Services.AddSingleton<ICommandInterceptor, RecordingInterceptor>();
    }

    /// <summary>A consumer interceptor; counts its calls in static slots, which is safe in this non-parallel collection.</summary>
    private sealed class RecordingInterceptor : ICommandInterceptor
    {
        public static int InterceptCalls { get; private set; }

        public static int ResultCalls { get; private set; }

        /// <summary>Reads the standard error console when <see cref="Intercept" /> runs, so a test can tell which interceptor went first.</summary>
        public static Func<string>? ErrorOutputProbe { get; set; }

        /// <summary>Whether the library's announcement was already on standard error when this interceptor ran.</summary>
        public static bool AnnouncementSeenFirst { get; private set; }

        public static void Reset()
        {
            InterceptCalls = 0;
            ResultCalls = 0;
            ErrorOutputProbe = null;
            AnnouncementSeenFirst = false;
        }

        public void Intercept(CommandContext context, CommandSettings settings)
        {
            InterceptCalls++;
            AnnouncementSeenFirst = ErrorOutputProbe?.Invoke().Contains("Executing command", StringComparison.Ordinal) == true;
        }

        public void InterceptResult(CommandContext context, CommandSettings settings, ref int result) => ResultCalls++;
    }

    /// <summary>
    ///     An <see cref="IAnsiConsole" /> that passes writes through until <see cref="FailWrites" /> is set, after which
    ///     every write fails, standing in for a standard error stream that closed mid-run.
    /// </summary>
    private sealed class ThrowingConsole(IAnsiConsole inner) : IAnsiConsole
    {
        public bool FailWrites { get; set; }

        public Profile Profile => inner.Profile;

        public IAnsiConsoleCursor Cursor => inner.Cursor;

        public IAnsiConsoleInput Input => inner.Input;

        public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;

        public RenderPipeline Pipeline => inner.Pipeline;

        public void Clear(bool home) => inner.Clear(home);

        public void Write(IRenderable renderable)
        {
            if (FailWrites)
            {
                throw new IOException("standard error is closed");
            }

            inner.Write(renderable);
        }
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

        /// <summary>The hosted services the container holds, so a test can tell "registered" from "started".</summary>
        public List<Type> HostedServiceTypes { get; } = [];

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
            recorder.HostedServiceTypes.AddRange(services.GetServices<IHostedService>().Select(service => service.GetType()));

            return 0;
        }
    }

    // Rejects itself when the registered validator asks it, which Spectre does through the command's Validate override
    // after binding, so the command is constructed but never executed. The message carries brackets so the tests can
    // see it is escaped rather than parsed as markup.
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RejectingSettings : CommandSettings
    {
        public override ValidationResult Validate() => ValidationResult.Error("rejected by Validate [brackets kept]");
    }

    /// <summary>
    ///     Registered for <see cref="RejectingSettings" />; Spectre constructs it and calls its <c>Validate</c>, which
    ///     rejects, so it never executes. It derives from the framework base so the registration mirrors a real command.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RejectingCommand(CommandArgumentsRootProcessor settingsProcessor,
                                          ICommandSettingsValidator<RejectingSettings> validator,
                                          IExceptionHandler exceptionHandler,
                                          IOutput output)
        : AppCommand<RejectingSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        /// <summary>Set by the command if it ever runs; the tests expect it never does.</summary>
        public static bool Executed { get; set; }

        protected override ExitCode DoExecute(CommandContext context, RejectingSettings settings, CancellationToken cancellationToken)
        {
            Executed = true;

            return ExitCode.Success;
        }
    }

    // Binds and validates itself fine; only the registered validator below rejects it.
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class AcceptingSettings : CommandSettings
    {
    }

    /// <summary>Constructed by Spectre through the host, which the inspection cannot see; must never execute in the tests.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class AcceptingCommand(CommandArgumentsRootProcessor settingsProcessor,
                                          ICommandSettingsValidator<AcceptingSettings> validator,
                                          IExceptionHandler exceptionHandler,
                                          IOutput output)
        : AppCommand<AcceptingSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        /// <summary>Set by the command if it ever runs; the tests expect it never does.</summary>
        public static bool Executed { get; set; }

        protected override ExitCode DoExecute(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken)
        {
            Executed = true;

            return ExitCode.Success;
        }
    }

    /// <summary>Replaces the default validator for <see cref="AcceptingSettings" /> and counts how often Spectre asks it.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RejectingValidator : ICommandSettingsValidator<AcceptingSettings>
    {
        public static int Calls { get; set; }

        public ValidationResult Validate(CommandContext context, AcceptingSettings settings)
        {
            Calls++;

            return ValidationResult.Error("rejected by the validator");
        }
    }

    /// <summary>Takes one required integer argument, so a bare invocation and a non-numeric value both fail while binding.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ArgumentSettings : CommandSettings
    {
        // Set by Spectre.Console.Cli through reflection while binding.
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [CommandArgument(0, "<number>")]
        public int Number { get; init; }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ArgumentCommand : Command<ArgumentSettings>
    {
        public override int Execute(CommandContext context, ArgumentSettings settings, CancellationToken cancellationToken) => settings.Number;
    }

    /// <summary>
    ///     A plain Spectre command, outside the framework base classes, so whatever it throws reaches Spectre's own
    ///     exception handling rather than the base class's catch. The factory is static because Spectre constructs the
    ///     command; safe in this non-parallel collection.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ThrowingPlainCommand(CancellationTokenSource cancellationTokenSource) : Command<AcceptingSettings>
    {
        /// <summary>Given the application's registered cancellation source, so a test can cancel it without capturing its own disposable.</summary>
        public static Func<CancellationTokenSource, Exception>? ExceptionFactory { get; set; }

        public override int Execute(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken) =>
            throw (ExceptionFactory ?? throw new InvalidOperationException("The exception factory was not set before the run."))(cancellationTokenSource);
    }

    /// <summary>Throws from <c>DoExecute</c>, so the base class routes the exception to the registered handler.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ThrowingAppCommand(CommandArgumentsRootProcessor settingsProcessor,
                                            ICommandSettingsValidator<AcceptingSettings> validator,
                                            IExceptionHandler exceptionHandler,
                                            IOutput output)
        : AppCommand<AcceptingSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        protected override ExitCode DoExecute(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("base class command failed");
    }

    /// <summary>The asynchronous counterpart of <see cref="ThrowingAppCommand" />.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ThrowingAsyncAppCommand(CommandArgumentsRootProcessor settingsProcessor,
                                                 ICommandSettingsValidator<AcceptingSettings> validator,
                                                 IExceptionHandler exceptionHandler,
                                                 IOutput output)
        : AsyncAppCommand<AcceptingSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken)
        {
            // Faults after the first suspension, so the exception travels through the task rather than out of the call.
            await Task.Yield();

            throw new InvalidOperationException("base class command failed");
        }
    }

    /// <summary>Cancels the application while being constructed and throws, so Spectre sees a wrapped cancellation.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class CancellingConstructorCommand(CancellationTokenSource cancellationTokenSource) : Command<AcceptingSettings>
    {
        private readonly CancellationToken _token = CancelAndThrow(cancellationTokenSource);

        public override int Execute(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken) => _token.IsCancellationRequested ? 1 : 0;

        private static CancellationToken CancelAndThrow(CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        }
    }

    /// <summary>Takes a dependency nothing registers, so the container cannot construct it.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class UnresolvableCommand(StringWriter dependency) : Command<AcceptingSettings>
    {
        public override int Execute(CommandContext context, AcceptingSettings settings, CancellationToken cancellationToken) => dependency.NewLine.Length;
    }

    /// <summary>Records what it is asked to handle; static state for the same reason as the commands above.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RecordingExceptionHandler : IExceptionHandler
    {
        public const int ReturnedExitCode = 77;

        public static List<Exception> Exceptions { get; } = [];

        public static bool Throws { get; set; }

        public static void Reset()
        {
            Exceptions.Clear();
            Throws = false;
        }

        public int HandleException(Exception ex)
        {
            Exceptions.Add(ex);

            if (Throws)
            {
                throw new InvalidOperationException("the handler failed");
            }

            return ReturnedExitCode;
        }
    }

    private sealed class MarkerServicesBundle : ServicesBundle
    {
        public override void DoConfigure() => Services.AddSingleton(new Marker());
    }

    /// <summary>
    ///     Counts the lifecycle calls a started host would make, and the disposal the host's own disposal causes. The
    ///     counters are static because the host constructs the instance; that is safe because this class runs in a
    ///     non-parallel collection and <see cref="AppBuilderTests.Dispose" /> resets them.
    /// </summary>
    private sealed class ProbeHostedService : IHostedService, IDisposable
    {
        public static int StartCalls { get; private set; }

        public static int StopCalls { get; private set; }

        public static int DisposeCalls { get; private set; }

        public static void Reset()
        {
            StartCalls = 0;
            StopCalls = 0;
            DisposeCalls = 0;
        }

        public void Dispose() => DisposeCalls++;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalls++;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalls++;

            return Task.CompletedTask;
        }
    }
}
