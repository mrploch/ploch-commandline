using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.UseCases;

public class ExportProjectUseCaseTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();

    [Fact]
    public async Task ExecuteAsync_should_write_a_manifest_for_an_existing_project()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var project = new ProjectItem("SpectreDemo", "Demo project", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            File.Exists(result.Value.OutputPath).Should().BeTrue();
            (await File.ReadAllTextAsync(result.Value.OutputPath, TestContext.Current.CancellationToken)).Should().Contain("SpectreDemo");
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    /// <summary>
    ///     The project name becomes a path segment, so a stored name containing a separator would let an export
    ///     requested for one directory write somewhere else entirely.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_reject_a_project_name_that_escapes_the_output_directory()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var escapedName = ".." + Path.DirectorySeparatorChar + "outside";
        var project = new ProjectItem(escapedName, "Traversal probe", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync(escapedName, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        var siblingPath = Path.GetFullPath(Path.Combine(outputPath, "..", "outside.json"));

        try
        {
            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest(escapedName, outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeFalse("a name that escapes the requested directory must not be exported");
            File.Exists(siblingPath).Should().BeFalse("nothing may be written outside the requested output directory");
        }
        finally
        {
            if (File.Exists(siblingPath))
            {
                File.Delete(siblingPath);
            }

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    /// <summary>
    ///     A containment check written as "does the relative path start with ..?" rejects this name, because
    ///     "..archive.json" does start with those two characters - while resolving to a file directly inside the
    ///     requested directory. Comparing the resolved parent directory instead is what keeps a legitimate leading
    ///     dot-dot working, so this test exists to stop that regressing to a prefix test.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_export_a_project_whose_name_begins_with_two_dots()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        const string dottedName = "..archive";
        var project = new ProjectItem(dottedName, "Leading dot-dot probe", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync(dottedName, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest(dottedName, outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeTrue("the name resolves inside the requested directory, so it is not an escape");
            File.Exists(Path.Join(outputPath, "..archive.json")).Should().BeTrue("the manifest belongs directly in the requested directory");
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_should_report_not_found_for_an_unknown_project()
    {
        _projectRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProjectItem?)null);

        var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
            .ExecuteAsync(new ExportProjectRequest("Missing", Path.GetTempPath()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
