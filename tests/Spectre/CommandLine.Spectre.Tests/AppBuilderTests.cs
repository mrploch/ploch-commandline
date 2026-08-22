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
///     <see cref="IHostBuilder" /> and <c>AddServicesBundle</c>, and the tests now pin that instead.
/// </remarks>
[Collection(GlobalConsoleState.Name)]
public sealed class AppBuilderTests : IDisposable
{
    /// <summary>Expected marker order for the additive-configuration tests; a field rather than a literal so CA1861 stays quiet.</summary>
    private static readonly string[] FirstThenSecond = ["first", "second"];

    /// <summary>Expected marker order when the two overloads of the same method are mixed.</summary>
    private static readonly string[] WithoutThenWithContext = ["without-context", "with-context"];

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
        var builder = new AppBuilder(appInfo, new CancellationTokenSource());

        builder.WithName("Widget Tool").Should().BeSameAs(builder, "the fluent methods chain");

        appInfo.Name.Should().Be("Widget Tool");
    }

    [Fact]
    public void WithVersion_should_set_the_version_used_by_the_start_up_banner()
    {
        var appInfo = new ConsoleAppInfo();
        var builder = new AppBuilder(appInfo, new CancellationTokenSource());

        builder.WithVersion(new Version(1, 2, 3)).Should().BeSameAs(builder);

        appInfo.Version.Should().Be(new Version(1, 2, 3));
    }

    [Fact]
    public void WithDescription_should_set_the_description_used_by_the_start_up_banner()
    {
        var appInfo = new ConsoleAppInfo();
        var builder = new AppBuilder(appInfo, new CancellationTokenSource());

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
        var builder = new AppBuilder(new ConsoleAppInfo(), new CancellationTokenSource());

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

    /// <summary>
    ///     Builds an application configured by <paramref name="configure" /> and runs a probe command through it.
    ///     The probe reports back through a static slot rather than a registered service, so that the helper's own
    ///     registrations stay out of the way of whatever the test configured — the marker assertions count exactly
    ///     the registrations the test made.
    /// </summary>
    private static ProbeRecorder RunProbeCommand(Action<AppBuilder> configure, CancellationTokenSource? cancellationTokenSource = null)
    {
        var recorder = new ProbeRecorder();
        ProbeCommand.Recorder = recorder;
        var builder = new AppBuilder(new ConsoleAppInfo { Name = "Probe App" }, cancellationTokenSource ?? new CancellationTokenSource());
        configure(builder);

        var executor = builder.ConfigureCommandApp(configurator => configurator.AddCommand<ProbeCommand>("probe"));

        recorder.ExitCode = executor.Run("probe");

        return recorder;
    }

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
