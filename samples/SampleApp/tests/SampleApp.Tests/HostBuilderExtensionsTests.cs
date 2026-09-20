using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests;

/// <summary>
///     Cover for the sample's configuration anchoring. The sample used to add its own
///     <c>appsettings.json</c> source to reach the deployment directory, which appended the file on top of the
///     environment variables and the command-line arguments the host had already layered above it, so a value given on
///     the command line was silently overridden by the file (issue #84). These tests pin both halves of the fix: the
///     settings file is still found when the process runs from an unrelated working directory, and the command line
///     still outranks it.
/// </summary>
public sealed class HostBuilderExtensionsTests : IDisposable
{
    private const string MaxBatchSizeKey = "SampleAppSettings:MaxBatchSize";

    private readonly string _deploymentDirectory = Directory.CreateTempSubdirectory("ploch-sample-settings-").FullName;

    public HostBuilderExtensionsTests() =>
        File.WriteAllText(Path.Combine(_deploymentDirectory, HostBuilderExtensions.SettingsFileName),
                          """{ "SampleAppSettings": { "Environment": "FromSettingsFile", "MaxBatchSize": 100 } }""");

    public void Dispose() => Directory.Delete(_deploymentDirectory, recursive: true);

    [Fact]
    public void UseSettingsFromDeploymentDirectory_should_let_a_command_line_argument_override_the_settings_file()
    {
        using var host = BuildHost($"--{MaxBatchSizeKey}=5");

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration[MaxBatchSizeKey].Should().Be("5", "the command line outranks appsettings.json");
    }

    [Fact]
    public void UseSettingsFromDeploymentDirectory_should_load_the_settings_file_from_the_deployment_directory()
    {
        using var host = BuildHost();

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["SampleAppSettings:Environment"]
            .Should()
            .Be("FromSettingsFile", "the file is found through the content root, not through the current working directory");
    }

    [Fact]
    public void UseSettingsFromDeploymentDirectory_should_throw_when_the_deployment_directory_holds_no_settings_file()
    {
        var emptyDirectory = Directory.CreateTempSubdirectory("ploch-sample-no-settings-").FullName;

        try
        {
            var act = () => Host.CreateDefaultBuilder().UseSettingsFromDeploymentDirectory(emptyDirectory);

            act.Should().Throw<FileNotFoundException>("a missing configuration file has to fail loudly at start-up");
        }
        finally
        {
            Directory.Delete(emptyDirectory, recursive: true);
        }
    }

    private IHost BuildHost(params string[] args) =>
        Host.CreateDefaultBuilder(args).UseSettingsFromDeploymentDirectory(_deploymentDirectory).Build();
}
