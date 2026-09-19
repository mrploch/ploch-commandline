using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Cover for the DI entry point. <c>AddSerilog</c> used to configure Serilog twice — once through the bundle and
///     once directly — and the second registration silently dropped the caller's output template.
/// </summary>
public sealed class SerilogLoggingConfiguratorTests : IDisposable
{
    private const decimal SampleAmount = 1234.5m;

    private const string Template = "REGISTERED|{Level:u3}|{Message:lj}{NewLine}";

    private readonly string _logDirectory = Path.Join(Path.GetTempPath(), "ploch-commandline-serilog-tests", Guid.NewGuid().ToString("N"));

    public SerilogLoggingConfiguratorTests() => Directory.CreateDirectory(_logDirectory);

    public void Dispose()
    {
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
    public void AddSerilog_should_register_the_serilog_logger_exactly_once()
    {
        var services = new ServiceCollection();

        services.AddSerilog(new ConfigurationBuilder().Build(), "registered", _logDirectory, Template);

        services.Count(descriptor => descriptor.ServiceType == typeof(ILogger))
                .Should()
                .Be(1, "a second registration would build the logger again and discard the first configuration");
    }

    [Fact]
    public void AddSerilog_should_return_the_same_service_collection_for_chaining()
    {
        var services = new ServiceCollection();

        services.AddSerilog(new ConfigurationBuilder().Build(), "chained", _logDirectory).Should().BeSameAs(services);
    }

    [Fact]
    public void AddSerilog_should_apply_the_supplied_template_and_log_name_to_the_resolved_logger()
    {
        var services = new ServiceCollection();
        services.AddSerilog(new ConfigurationBuilder().Build(), "registered", _logDirectory, Template);

        using (var provider = services.BuildServiceProvider())
        {
            var logger = provider.GetRequiredService<ILogger>();
            logger.Information("a registered message");
            (logger as IDisposable)?.Dispose();
        }

        ReadLogFile("registered.log").Should().Contain("REGISTERED|INF|a registered message", "the template the caller passed must survive registration");
    }

    [Fact]
    public void AddSerilog_should_pass_the_supplied_culture_through_to_the_log_files()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var expected = string.Format(culture, "amount {0:N2}", SampleAmount);

        // The ambient culture is invariant, so a German rendering can only have come from the supplied culture.
        AmbientCulture.Run(CultureInfo.InvariantCulture.Name, () => WriteThroughRegisteredLogger("formatted", culture));

        expected.Should().NotBe(string.Format(CultureInfo.InvariantCulture, "amount {0:N2}", SampleAmount));
        ReadLogFile("formatted.log").Should().Contain(expected, "the culture the caller passed must reach the file sinks");
        ReadLogFile("formatted-errors.log").Should().Contain(expected, "the culture applies to the error log as well");
    }

    [Fact]
    public void AddSerilog_should_write_invariant_values_when_no_culture_is_supplied()
    {
        var expected = string.Format(CultureInfo.InvariantCulture, "amount {0:N2}", SampleAmount);

        AmbientCulture.Run("de-DE", () => WriteThroughRegisteredLogger("invariant"));

        ReadLogFile("invariant.log").Should().Contain(expected, "log files default to the invariant culture");
        ReadLogFile("invariant-errors.log").Should().Contain(expected, "the error log defaults to the invariant culture as well");
    }

    /// <summary>Registers Serilog through <c>AddSerilog</c> and writes one warning, which reaches both log files.</summary>
    /// <param name="logName">The base name of the log files.</param>
    /// <param name="culture">The culture passed to the registration, or <see langword="null" /> for the default.</param>
    private void WriteThroughRegisteredLogger(string logName, CultureInfo? culture = null)
    {
        var services = new ServiceCollection();
        services.AddSerilog(new ConfigurationBuilder().Build(), logName, _logDirectory, Template, culture);

        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger>();
        logger.Warning("amount {Amount:N2}", SampleAmount);
        (logger as IDisposable)?.Dispose();
    }

    private string ReadLogFile(string fileName)
    {
        var path = Path.Join(_logDirectory, fileName);
        File.Exists(path).Should().BeTrue($"the registration is expected to create {fileName}");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
