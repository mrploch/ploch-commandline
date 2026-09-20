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
///     settings file is read from the deployment directory rather than the working directory, and the command line
///     still outranks it.
/// </summary>
public sealed class HostBuilderExtensionsTests : IDisposable
{
    private const string MaxBatchSizeKey = "SampleAppSettings:MaxBatchSize";

    private const string EnvironmentKey = "SampleAppSettings:Environment";

    /// <summary>Value written only into the deployment directory, so reading it proves which file was loaded.</summary>
    private const string DeploymentDirectoryMarker = "FromDeploymentDirectory";

    private readonly string _deploymentDirectory = Directory.CreateTempSubdirectory("ploch-sample-settings-").FullName;

    public HostBuilderExtensionsTests() =>
        File.WriteAllText(Path.Join(_deploymentDirectory, HostBuilderExtensions.SettingsFileName),
                          $$"""{ "SampleAppSettings": { "Environment": "{{DeploymentDirectoryMarker}}", "MaxBatchSize": 100 } }""");

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
        // The working directory of the test run holds an appsettings.json of its own, copied from the sample project,
        // whose Environment is "Development". Resolving against the working directory - the host's default, and the
        // trap this guards against - would therefore read that value rather than the marker below. Asserting on the
        // difference keeps the test honest without mutating Directory.SetCurrentDirectory, which is process-global
        // state shared with every other test class in the assembly.
        var workingDirectoryCopy = Path.Join(Directory.GetCurrentDirectory(), HostBuilderExtensions.SettingsFileName);
        File.Exists(workingDirectoryCopy).Should().BeTrue("the working directory needs a competing settings file for this test to discriminate");
        File.ReadAllText(workingDirectoryCopy).Should().NotContain(DeploymentDirectoryMarker, "the two files have to disagree");

        using var host = BuildHost();

        host.Services.GetRequiredService<IConfiguration>()[EnvironmentKey]
            .Should()
            .Be(DeploymentDirectoryMarker, "the file is found through the content root, not through the working directory");

        Path.TrimEndingDirectorySeparator(host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath)
            .Should()
            .Be(Path.TrimEndingDirectorySeparator(_deploymentDirectory));
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
