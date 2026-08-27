using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Cover for the predefined Serilog configuration. The important behaviour is the split between the two files:
///     the dedicated "errors" log must receive warnings and above only. The error sink previously sat outside its
///     filtered sub-logger, which left the filter applying to nothing and the errors file receiving every event.
/// </summary>
public sealed class LoggerConfigurationExtensionsTests : IDisposable
{
    private readonly string _logDirectory = Path.Join(Path.GetTempPath(), "ploch-commandline-serilog-tests", Guid.NewGuid().ToString("N"));

    public LoggerConfigurationExtensionsTests() => Directory.CreateDirectory(_logDirectory);

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
    public void ConfigureSerilog_should_write_every_level_to_the_main_log_file()
    {
        WriteSampleEvents("main-levels");

        var mainLog = ReadLogFile("main-levels.log");

        mainLog.Should().Contain("an informational message").And.Contain("a warning message").And.Contain("an error message");
    }

    [Fact]
    public void ConfigureSerilog_should_send_only_warnings_and_above_to_the_errors_log_file()
    {
        WriteSampleEvents("filtered");

        var errorLog = ReadLogFile("filtered-errors.log");

        errorLog.Should()
                .Contain("a warning message")
                .And.Contain("an error message")
                .And.Contain("a fatal message")
                .And.NotContain("an informational message", "the errors file exists precisely to exclude routine events");
    }

    [Fact]
    public void ConfigureSerilog_should_honour_the_minimum_level_taken_from_configuration()
    {
        var configuration = BuildConfiguration(("Serilog:MinimumLevel:Default", "Warning"));

        WriteSampleEvents("minimum-level", configuration);

        var mainLog = ReadLogFile("minimum-level.log");

        mainLog.Should().NotContain("an informational message", "the configured minimum level suppresses it").And.Contain("a warning message");
    }

    [Fact]
    public void ConfigureSerilog_should_default_to_information_when_configuration_gives_no_minimum_level()
    {
        WriteSampleEvents("default-level", BuildConfiguration());

        var mainLog = ReadLogFile("default-level.log");

        mainLog.Should().Contain("an informational message").And.NotContain("a debug message", "Information is the default floor");
    }

    [Fact]
    public void ConfigureSerilog_should_apply_the_supplied_output_template_to_the_main_log_file()
    {
        WriteSampleEvents("templated", template: "CUSTOM|{Level:u3}|{Message:lj}{NewLine}");

        var mainLog = ReadLogFile("templated.log");

        mainLog.Should().Contain("CUSTOM|INF|an informational message", "the template replaces the default timestamped layout");
    }

    [Fact]
    public void ConfigureSerilog_should_name_the_log_files_after_the_current_process_when_no_name_is_supplied()
    {
        using (var logger = new LoggerConfiguration().ConfigureSerilog(logPath: _logDirectory).CreateLogger())
        {
            logger.Information("an informational message");
            logger.Error("an error message");
        }

        var processName = Process.GetCurrentProcess().ProcessName;
        File.Exists(Path.Join(_logDirectory, $"{processName}.log")).Should().BeTrue();
        File.Exists(Path.Join(_logDirectory, $"{processName}-errors.log")).Should().BeTrue();
    }

    /// <summary>
    ///     logName shapes a file path, so a rooted value used to make Path.Combine discard logPath entirely and
    ///     write to the root instead - the library silently ignoring the very parameter documented to control where
    ///     logs go. The test asserts the negative that matters: nothing appears at the rooted location.
    /// </summary>
    [Fact]
    public void ConfigureSerilog_should_not_write_outside_the_log_directory_for_a_rooted_log_name()
    {
        var rootedName = Path.Join(Path.GetTempPath(), $"escaped-{Guid.NewGuid():N}");
        var escapedPath = $"{rootedName}.log";

        try
        {
            using (var logger = new LoggerConfiguration().ConfigureSerilog(logName: rootedName, logPath: _logDirectory).CreateLogger())
            {
                logger.Information("an informational message");
            }

            File.Exists(escapedPath).Should().BeFalse("a rooted logName must not override the configured logPath");
        }
        finally
        {
            if (File.Exists(escapedPath))
            {
                File.Delete(escapedPath);
            }
        }
    }

    /// <summary>
    ///     The sibling test above covers a <em>rooted</em> logName. This covers the other escape, which Path.Join
    ///     alone does not close: a relative name carrying ".." survives concatenation as "logs/../outside.log" and
    ///     the operating system resolves it to a sibling of the configured directory.
    /// </summary>
    [Theory]
    [InlineData("/")]
    [InlineData("\\")]
    public void ConfigureSerilog_should_not_write_outside_the_log_directory_for_a_traversing_log_name(string separator)
    {
        // Both cases resolve to the same sibling file on Windows, where '/' and '\' are equivalent separators,
        // so the destination is made unique per case - otherwise the two theory rows race for one path and the
        // result depends on execution order rather than on the code under test.
        var traversingName = $"..{separator}escaped-{Guid.NewGuid():N}";
        var siblingPath = Path.GetFullPath(Path.Join(_logDirectory, $"{traversingName}.log"));

        try
        {
            using (var logger = new LoggerConfiguration().ConfigureSerilog(logName: traversingName, logPath: _logDirectory).CreateLogger())
            {
                logger.Information("an informational message");
            }

            File.Exists(siblingPath).Should().BeFalse("a logName carrying '..' must not escape the configured logPath");
        }
        finally
        {
            if (File.Exists(siblingPath))
            {
                File.Delete(siblingPath);
            }
        }
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value)).Build();

    private void WriteSampleEvents(string logName, IConfiguration? configuration = null, string? template = null)
    {
        using var logger = new LoggerConfiguration().ConfigureSerilog(configuration, template, logName, _logDirectory).CreateLogger();

        logger.Write(LogEventLevel.Debug, "a debug message");
        logger.Write(LogEventLevel.Information, "an informational message");
        logger.Write(LogEventLevel.Warning, "a warning message");
        logger.Write(LogEventLevel.Error, "an error message");
        logger.Write(LogEventLevel.Fatal, "a fatal message");
    }

    /// <summary>Reads a log file while the sink may still hold it open.</summary>
    private string ReadLogFile(string fileName)
    {
        var path = Path.Join(_logDirectory, fileName);
        File.Exists(path).Should().BeTrue($"the configuration is expected to create {fileName}");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
