using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Serilog.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using ILogger = Serilog.ILogger;

namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Cover for the <see cref="AppBuilder" /> entry point. Serilog is opt-in (PLO-778): the core package registers no
///     Serilog service, so the registration <see cref="AppBuilderSerilogExtensions.UseSerilog" /> queues is the only
///     one in the host, which is what PLO-765 asked for — a consumer's own call used to be a second registration on
///     top of the one the core bundle made.
/// </summary>
[Collection(HostedSerilogState.Name)]
public sealed class AppBuilderSerilogExtensionsTests : IDisposable
{
    private const string Template = "HOSTED|{Level:u3}|{Message:lj}{NewLine}";

    private const decimal SampleAmount = 1234.5m;

    private readonly string _logDirectory = Path.Join(Path.GetTempPath(), "ploch-commandline-serilog-tests", Guid.NewGuid().ToString("N"));

    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;

    private readonly StringWriter _consoleWriter = new(CultureInfo.InvariantCulture);

    private readonly IAnsiConsole _console;

    private readonly ProbeRecorder _recorder = new();

    public AppBuilderSerilogExtensionsTests()
    {
        Directory.CreateDirectory(_logDirectory);
        _console = AnsiConsole.Create(new AnsiConsoleSettings
                                      {
                                          Ansi = AnsiSupport.No,
                                          ColorSystem = ColorSystemSupport.NoColors,
                                          Interactive = InteractionSupport.No,
                                          Out = new AnsiConsoleOutput(_consoleWriter)
                                      });
        AnsiConsole.Console = _console;
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _consoleWriter.Dispose();

        try
        {
            Directory.Delete(_logDirectory, recursive: true);
        }
        catch (IOException exception)
        {
            // A sink that has not released its handle yet must not fail the test; the directory is under TEMP.
            Console.WriteLine($"Could not remove the temporary log directory: {exception.Message}");
        }
    }

    [Fact]
    public void UseSerilog_should_return_the_same_builder_for_chaining()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var builder = new AppBuilder(new ConsoleAppInfo { Name = "Chaining Probe" }, cancellationTokenSource);

        builder.UseSerilog(logName: "chained", logPath: _logDirectory).Should().BeSameAs(builder);
    }

    [Fact]
    public void UseSerilog_should_throw_when_the_builder_is_null()
    {
        var registration = () => ((AppBuilder)null!).UseSerilog();

        registration.Should().Throw<ArgumentNullException>().WithParameterName("appBuilder");
    }

    [Fact]
    public void UseSerilog_should_register_the_serilog_logger_exactly_once()
    {
        RunProbe(builder => builder.UseSerilog(logName: "once", logPath: _logDirectory, template: Template));

        _recorder.SerilogLoggerDescriptors.Should().Be(1, "the core package no longer registers Serilog, so the opt-in call is the only registration (PLO-765)");
    }

    [Fact]
    public void AddSerilog_from_configure_services_should_register_the_serilog_logger_exactly_once()
    {
        // The route the documentation showed before UseSerilog existed must give the same single registration.
        RunProbe(builder => builder.ConfigureServices((context, services) =>
                                                          services.AddSerilog(context.Configuration, "configured", _logDirectory, Template)));

        _recorder.SerilogLoggerDescriptors.Should().Be(1, "a consumer's AddSerilog is no longer a second registration on top of the core bundle (PLO-765)");
    }

    [Fact]
    public void UseSerilog_should_make_serilog_the_logger_factory_of_the_host()
    {
        RunProbe(builder => builder.UseSerilog(logName: "factory", logPath: _logDirectory, template: Template));

        _recorder.ExitCode.Should().Be((int)ExitCode.Success);
        _recorder.LoggerFactory.Should().BeOfType<SerilogLoggerFactory>("the resolved factory is Serilog's, whatever the host registered by default");
    }

    [Fact]
    public void UseSerilog_should_write_a_command_log_message_to_the_configured_file()
    {
        RunProbe(builder => builder.UseSerilog(logName: "written", logPath: _logDirectory, template: Template));

        var contents = ReadLogFile("written.log");

        contents.Should().Contain("HOSTED|INF|the probe ran", "the command's ILogger<T> message reaches the rolling file with the supplied template");
        contents.Split("HOSTED|INF|the probe ran").Should().HaveCount(2, "the message is written exactly once");
    }

    [Fact]
    public void UseSerilog_should_pass_the_supplied_culture_through_to_the_log_file()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var expected = string.Format(culture, "amount {0:N2}", SampleAmount);

        RunProbe(builder => builder.UseSerilog(logName: "cultured", logPath: _logDirectory, template: Template, culture: culture));

        expected.Should().NotBe(string.Format(CultureInfo.InvariantCulture, "amount {0:N2}", SampleAmount));
        ReadLogFile("cultured.log").Should().Contain(expected, "the culture the caller passed must reach the file sink");
    }

    [Fact]
    public void UseSerilog_should_read_the_serilog_section_of_the_host_configuration()
    {
        // The predefined minimum level is Information, so only a configuration that was actually forwarded can
        // suppress the Information line while the Warning line still goes through.
        RunProbe(builder => builder.ConfigureAppConfiguration(configuration =>
                                                                  configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                                  {
                                                                      ["Serilog:MinimumLevel:Default"] = "Warning"
                                                                  }))
                                   .UseSerilog(logName: "configured-level", logPath: _logDirectory, template: Template));

        var contents = ReadLogFile("configured-level.log");

        contents.Should().Contain("HOSTED|WRN|the probe warned");
        contents.Should().NotContain("the probe ran", "the Warning minimum level from the host configuration filters the Information line");
    }

    /// <summary>
    ///     Configures a builder with the supplied opt-in, runs the probe command and records what it observed.
    /// </summary>
    /// <param name="optIn">The Serilog registration under test.</param>
    private void RunProbe(Action<AppBuilder> optIn)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var builder = new AppBuilder(new ConsoleAppInfo { Name = "Serilog Probe" }, cancellationTokenSource);
        builder.ConfigureServices(services => services.AddSingleton(_recorder));
        optIn(builder);

        // Queued after the opt-in, so by the time it runs the Serilog registrations are in the collection.
        builder.ConfigureServices(services => _recorder.SerilogLoggerDescriptors = services.Count(descriptor => descriptor.ServiceType == typeof(ILogger)));

        var executor = builder.ConfigureCommandApp(configurator =>
                                                   {
                                                       configurator.Settings.Console = _console;
                                                       configurator.AddCommand<SerilogProbeCommand>("probe");
                                                   });

        // Spectre disposes the type resolver, and with it the host and the Serilog sinks, when the run completes.
        _recorder.ExitCode = executor.Run("probe");
    }

    private string ReadLogFile(string fileName)
    {
        var path = Path.Join(_logDirectory, fileName);
        File.Exists(path).Should().BeTrue($"the registration is expected to create {fileName}");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    private sealed class ProbeRecorder
    {
        public int ExitCode { get; set; } = int.MinValue;

        public int SerilogLoggerDescriptors { get; set; } = int.MinValue;

        public ILoggerFactory? LoggerFactory { get; set; }
    }

    /// <summary>Takes <see cref="ILogger{TCategoryName}" /> exactly as a consumer's command would.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local - Spectre resolves it through the container.
    private sealed class SerilogProbeCommand(CommandArgumentsRootProcessor settingsProcessor,
                                             ICommandSettingsValidator<EmptyCommandSettings> validator,
                                             IExceptionHandler exceptionHandler,
                                             IOutput output,
                                             ILogger<SerilogProbeCommand> logger,
                                             ILoggerFactory loggerFactory,
                                             ProbeRecorder recorder)
        : AppCommand<EmptyCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
    {
        protected override ExitCode DoExecute(CommandContext context, EmptyCommandSettings settings, CancellationToken cancellationToken)
        {
            recorder.LoggerFactory = loggerFactory;
            logger.LogInformation("the probe ran");
            logger.LogWarning("the probe warned");
            logger.LogWarning("amount {Amount:N2}", SampleAmount);

            return ExitCode.Success;
        }
    }
}
