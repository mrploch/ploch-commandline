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
    private const string Template = "REGISTERED|{Level:u3}|{Message:lj}{NewLine}";

    private readonly string _logDirectory = Path.Combine(Path.GetTempPath(), "ploch-commandline-serilog-tests", Guid.NewGuid().ToString("N"));

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

    private string ReadLogFile(string fileName)
    {
        var path = Path.Combine(_logDirectory, fileName);
        File.Exists(path).Should().BeTrue($"the registration is expected to create {fileName}");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
