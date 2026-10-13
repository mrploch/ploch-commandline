using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     End-to-end cover for the lifetimes Spectre's registrations get once a command actually runs through the real
///     <c>DependencyInjectionTypeRegistrar</c>: the command and its settings are transient, the settings-processing
///     pipeline is transient, and the shared infrastructure stays singleton (PLO-781).
/// </summary>
/// <remarks>
///     Each test makes its own probe run. Spectre builds the host once per run, so the probe command observes the
///     container from the inside and records what two resolutions of each service hand back.
/// </remarks>
[Collection(GlobalConsoleState.Name)]
public sealed class AppBuilderLifetimeTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly RecordingConsole _console = new();
    private readonly LifetimeRecorder _recorder = new();

    public AppBuilderLifetimeTests()
    {
        AnsiConsole.Console = _console.Console;
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        EnvironmentSettings.Reset();
        _console.Dispose();
    }

    [Fact]
    public void Run_should_execute_the_probe_command()
    {
        RunProbe();

        _recorder.ExitCode.Should().Be((int)ExitCode.Success);
    }

    [Fact]
    public void Run_should_resolve_a_different_command_instance_for_every_resolution()
    {
        RunProbe();

        _recorder.CommandResolvedFromContainer.Should()
                 .NotBeNull()
                 .And.NotBeSameAs(_recorder.ExecutingCommand, "Spectre registers the command type as transient through the registrar");
    }

    [Fact]
    public void Run_should_resolve_a_different_settings_instance_for_every_resolution()
    {
        RunProbe();

        _recorder.SettingsResolvedFromContainer.Should()
                 .NotBeNull()
                 .And.NotBeSameAs(_recorder.ExecutingSettings, "Spectre registers the settings type as transient through the registrar");
    }

    [Fact]
    public void Run_should_hand_the_command_the_bound_settings_and_the_container_an_unbound_instance()
    {
        RunProbe();

        _recorder.ExecutingSettings!.Tag.Should().Be("bound", "Spectre binds the parsed arguments to the instance it passes to Execute");
        _recorder.SettingsResolvedFromContainer!.Tag.Should().BeNull("a settings instance resolved from the container is a fresh, unbound one");
    }

    [Fact]
    public void Run_should_resolve_a_fresh_settings_processor_for_every_resolution()
    {
        RunProbe();

        _recorder.FirstProcessor.Should().NotBeNull().And.NotBeSameAs(_recorder.SecondProcessor);
    }

    [Fact]
    public void Run_should_share_the_output_between_resolutions()
    {
        RunProbe();

        _recorder.FirstOutput.Should().NotBeNull().And.BeSameAs(_recorder.SecondOutput, "the output is shared infrastructure and stays a singleton");
    }

    [Fact]
    public void Run_should_share_the_cancellation_source_between_resolutions()
    {
        RunProbe();

        _recorder.FirstCancellationTokenSource.Should()
                 .NotBeNull()
                 .And.BeSameAs(_recorder.SecondCancellationTokenSource, "the application's cancellation source stays a singleton");
    }

    private void RunProbe()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var builder = new AppBuilder(new ConsoleAppInfo { Name = "Lifetime Probe" }, cancellationTokenSource);
        builder.ConfigureServices(services => services.AddSingleton(_recorder));

        var executor = builder.ConfigureCommandApp(configurator =>
                                                   {
                                                       // Spectre memoises AnsiConsole.Console process-wide the first time it needs a console, so a
                                                       // later run would resolve IAnsiConsole to the console of whichever test ran first. Handing it the
                                                       // console explicitly is the route Spectre provides for exactly that.
                                                       configurator.Settings.Console = _console.Console;
                                                       configurator.AddCommand<LifetimeProbeCommand>("probe");
                                                   });

        _recorder.ExitCode = executor.Run("probe", "--tag", "bound");
    }

    private sealed class LifetimeRecorder
    {
        public int ExitCode { get; set; } = int.MinValue;

        public LifetimeProbeCommand? ExecutingCommand { get; set; }

        public LifetimeProbeCommand? CommandResolvedFromContainer { get; set; }

        public LifetimeProbeSettings? ExecutingSettings { get; set; }

        public LifetimeProbeSettings? SettingsResolvedFromContainer { get; set; }

        public CommandArgumentsRootProcessor? FirstProcessor { get; set; }

        public CommandArgumentsRootProcessor? SecondProcessor { get; set; }

        public IOutput? FirstOutput { get; set; }

        public IOutput? SecondOutput { get; set; }

        public CancellationTokenSource? FirstCancellationTokenSource { get; set; }

        public CancellationTokenSource? SecondCancellationTokenSource { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Local - Spectre binds it through the container.
    private sealed class LifetimeProbeSettings : CommandSettings
    {
        [CommandOption("--tag")]
        public string? Tag { get; init; }
    }

    /// <summary>
    ///     Derives from <see cref="AppCommand{TSettings}" /> so the run exercises the framework pipeline the library
    ///     registers, not just Spectre's own resolution of the command.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local - Spectre resolves it through the container.
    private sealed class LifetimeProbeCommand(CommandArgumentsRootProcessor settingsProcessor,
                                              ICommandSettingsValidator<LifetimeProbeSettings> validator,
                                              IExceptionHandler exceptionHandler,
                                              IOutput output,
                                              IServiceProvider services,
                                              LifetimeRecorder recorder)
        : AppCommand<LifetimeProbeSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        protected override ExitCode DoExecute(CommandContext context, LifetimeProbeSettings settings, CancellationToken cancellationToken)
        {
            recorder.ExecutingCommand = this;
            recorder.CommandResolvedFromContainer = services.GetRequiredService<LifetimeProbeCommand>();
            recorder.ExecutingSettings = settings;
            recorder.SettingsResolvedFromContainer = services.GetRequiredService<LifetimeProbeSettings>();
            recorder.FirstProcessor = services.GetRequiredService<CommandArgumentsRootProcessor>();
            recorder.SecondProcessor = services.GetRequiredService<CommandArgumentsRootProcessor>();
            recorder.FirstOutput = services.GetRequiredService<IOutput>();
            recorder.SecondOutput = services.GetRequiredService<IOutput>();
            recorder.FirstCancellationTokenSource = services.GetRequiredService<CancellationTokenSource>();
            recorder.SecondCancellationTokenSource = services.GetRequiredService<CancellationTokenSource>();

            return ExitCode.Success;
        }
    }
}
