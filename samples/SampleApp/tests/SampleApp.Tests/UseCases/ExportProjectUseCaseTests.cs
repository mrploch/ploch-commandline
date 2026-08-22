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

    [Fact]
    public async Task ExecuteAsync_should_report_not_found_for_an_unknown_project()
    {
        _projectRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProjectItem?)null);

        var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
            .ExecuteAsync(new ExportProjectRequest("Missing", Path.GetTempPath()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
