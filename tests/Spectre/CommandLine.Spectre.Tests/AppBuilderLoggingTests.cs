using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for logging in an application that references only the core package. Serilog is opt-in, so a command
///     that takes <see cref="ILogger{TCategoryName}" /> must resolve it from the host's default providers, and nothing
///     the core assembly references or loads may be Serilog (PLO-778).
/// </summary>
/// <remarks>
///     In the console-state collection because the run swaps <c>AnsiConsole.Console</c> for a recording console.
/// </remarks>
[Collection(GlobalConsoleState.Name)]
public sealed class AppBuilderLoggingTests : IDisposable
{
    private const string SerilogIntegrationAssemblyName = "Ploch.CommandLine.Spectre.Serilog";

    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly RecordingConsole _console = new();
    private readonly LoggingRecorder _recorder = new();
    private readonly RecordingLoggerProvider _loggerProvider = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="AppBuilderLoggingTests" /> class with an isolated console.
    /// </summary>
    public AppBuilderLoggingTests() => AnsiConsole.Console = _console.Console;

    /// <summary>
    ///     Restores the global console and disposes the recording console.
    /// </summary>
    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _console.Dispose();
    }

    /// <summary>
    ///     Verifies that the core assembly compiles against neither Serilog nor the Serilog integration package.
    /// </summary>
    [Fact]
    public void Core_assembly_should_not_reference_serilog()
    {
        var referencedAssemblies = typeof(AppBuilder).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();

        referencedAssemblies.Should().NotContain(name => IsSerilogAssembly(name), "Serilog is opt-in through the Serilog package (PLO-778)");
    }

    /// <summary>
    ///     Verifies that a command resolves and uses <see cref="ILogger{TCategoryName}" /> without Serilog.
    /// </summary>
    [Fact]
    public void Run_should_resolve_a_logger_for_a_command_without_serilog()
    {
        RunProbe();

        _recorder.ExitCode.Should().Be((int)ExitCode.Success);
        _recorder.LoggerFactory.Should().BeOfType<LoggerFactory>("the host's default logging infrastructure serves the command, not a null factory");
        _loggerProvider.Messages.Should().ContainSingle("a provider added through the host's logging builder receives the command's event, so the pipeline is live")
                       .Which.Should().Be("[LoggingProbeCommand] The probe ran without Serilog");
    }

    /// <summary>
    ///     Verifies that running a command loads no Serilog assembly into the process.
    /// </summary>
    [Fact]
    public void Run_should_not_load_a_serilog_assembly()
    {
        RunProbe();

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetName().Name).ToArray();

        loadedAssemblies.Should().NotContain(name => IsSerilogAssembly(name), "a core-only application runs with no Serilog assembly loaded (PLO-778)");
    }

    private static bool IsSerilogAssembly(string? name) =>
        name is not null &&
        (name.Equals("Serilog", StringComparison.OrdinalIgnoreCase) ||
         name.StartsWith("Serilog.", StringComparison.OrdinalIgnoreCase) ||
         name.Equals(SerilogIntegrationAssemblyName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Runs the probe command and records its exit code and what it observed about logging.
    /// </summary>
    private void RunProbe()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var builder = new AppBuilder(new ConsoleAppInfo { Name = "Logging Probe" }, cancellationTokenSource);

        // The recording provider sits next to the host's default providers; the console provider would print to the
        // test output, so what the pipeline delivered is asserted through the recorder instead.
        builder.ConfigureServices(services => services.AddSingleton(_recorder).AddLogging(logging => logging.AddProvider(_loggerProvider)));

        var executor = builder.ConfigureCommandApp(configurator =>
                                                   {
                                                       // Spectre memoises AnsiConsole.Console process-wide the first time it needs a console, so the
                                                       // recording console is handed over explicitly rather than through the global.
                                                       configurator.Settings.Console = _console.Console;
                                                       configurator.AddCommand<LoggingProbeCommand>("probe");
                                                   });

        _recorder.ExitCode = executor.Run("probe");
    }

    private sealed class LoggingRecorder
    {
        public int ExitCode { get; set; } = int.MinValue;

        public ILoggerFactory? LoggerFactory { get; set; }
    }

    /// <summary>A logging provider that keeps every formatted message it is handed.</summary>
    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                messages.Add(formatter(state, exception));
        }
    }

    /// <summary>
    ///     Takes <see cref="ILogger{TCategoryName}" /> in its constructor exactly as a consumer's command would.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local - Spectre resolves it through the container.
    private sealed class LoggingProbeCommand(CommandArgumentsRootProcessor settingsProcessor,
                                             ICommandSettingsValidator<EmptyCommandSettings> validator,
                                             IExceptionHandler exceptionHandler,
                                             IOutput output,
                                             ILogger<LoggingProbeCommand> logger,
                                             ILoggerFactory loggerFactory,
                                             LoggingRecorder recorder)
        : AppCommand<EmptyCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        /// <summary>
        ///     Logs one message and records the factory that served the command.
        /// </summary>
        /// <param name="context">The context supplied by Spectre for the probe invocation.</param>
        /// <param name="settings">The settings of the probe, which has none.</param>
        /// <param name="cancellationToken">The cancellation token supplied to command execution.</param>
        /// <returns>The success exit code.</returns>
        protected override ExitCode DoExecute(CommandContext context, EmptyCommandSettings settings, CancellationToken cancellationToken)
        {
            recorder.LoggerFactory = loggerFactory;
            logger.LogInformation("[LoggingProbeCommand] The probe ran without Serilog");

            return ExitCode.Success;
        }
    }
}
