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
///     settings file is read from the deployment directory, and the command line still outranks it.
/// </summary>
/// <remarks>
///     The tests are isolated from the machine they run on. The settings file lives in a private temporary directory
///     rather than the test working directory, so nothing ambient can supply it and a crashed run cannot leave one
///     behind; and the key each test reads is unique to the test instance, so no environment variable inherited from
///     an IDE or a CI runner can collide with it. Nothing here mutates process-wide state - in particular the current
///     directory is left alone, because the other test classes in this assembly run in parallel with this one and some
///     of them use relative paths.
/// </remarks>
public sealed class HostBuilderExtensionsTests : IDisposable
{
    /// <summary>Value written only into the deployment directory, so reading it back proves which file was loaded.</summary>
    private const string DeploymentDirectoryMarker = "FromDeploymentDirectory";

    private readonly string _deploymentDirectory = Directory.CreateTempSubdirectory("ploch-sample-settings-").FullName;

    private readonly string _probeKey = $"SampleAppSettings:Probe{Guid.NewGuid():N}";

    public HostBuilderExtensionsTests() =>
        File.WriteAllText(Path.Join(_deploymentDirectory, HostBuilderExtensions.SettingsFileName),
                          $$"""{ "SampleAppSettings": { "{{ProbeName}}": "{{DeploymentDirectoryMarker}}" } }""");

    /// <summary>The probe key without its <c>SampleAppSettings:</c> section prefix, as it appears in the JSON file.</summary>
    private string ProbeName => _probeKey["SampleAppSettings:".Length..];

    public void Dispose() => Directory.Delete(_deploymentDirectory, recursive: true);

    [Fact]
    public void UseSettingsFromDeploymentDirectory_should_let_a_command_line_argument_override_the_settings_file()
    {
        using var host = BuildHost($"--{_probeKey}=FromCommandLine");

        host.Services.GetRequiredService<IConfiguration>()[_probeKey]
            .Should()
            .Be("FromCommandLine", "the command line outranks appsettings.json");
    }

    [Fact]
    public void UseSettingsFromDeploymentDirectory_should_load_the_settings_file_from_the_deployment_directory()
    {
        using var host = BuildHost();

        // The host resolves its JSON sources through a file provider rooted at the content root, so these two
        // assertions together are the proof: the content root is the deployment directory, and the value read back is
        // one that exists in no file anywhere else. Had the lookup fallen back to the working directory - the host's
        // default, and the trap this guards against - the probe key would have resolved to null.
        Path.TrimEndingDirectorySeparator(host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath)
            .Should()
            .Be(Path.TrimEndingDirectorySeparator(_deploymentDirectory));

        host.Services.GetRequiredService<IConfiguration>()[_probeKey]
            .Should()
            .Be(DeploymentDirectoryMarker, "the file is found through the content root, not through the working directory");
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
