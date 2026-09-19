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
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
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
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var escapedName = ".." + Path.DirectorySeparatorChar + "outside";
        var project = new ProjectItem(escapedName, "Traversal probe", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync(escapedName, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        var siblingPath = Path.GetFullPath(Path.Join(outputPath, "..", "outside.json"));

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
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
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

    /// <summary>
    ///     The symlink-safe write goes through a temporary file and a rename; that must still replace an earlier
    ///     manifest rather than failing because the destination already exists, and must not leave the temporary
    ///     file behind.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_overwrite_an_earlier_export_of_the_same_project()
    {
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var manifestPath = Path.Join(outputPath, "SpectreDemo.json");
        var project = new ProjectItem("SpectreDemo", "Second export", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(manifestPath, "stale manifest from an earlier export", TestContext.Current.CancellationToken);

            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeTrue("re-exporting a project replaces its previous manifest");
            var contents = await File.ReadAllTextAsync(manifestPath, TestContext.Current.CancellationToken);
            contents.Should().Contain("Second export").And.NotContain("stale manifest");
            Directory.GetFiles(outputPath).Should().ContainSingle("the temporary file must be renamed away, not left behind");
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
    ///     Regression test for issue #49: a symbolic link planted at the destination file used to be followed, so the
    ///     export truncated whatever the link pointed at. The link must be replaced instead, leaving its target intact.
    /// </summary>
    /// <remarks>
    ///     Creating a symbolic link on Windows needs Developer Mode or an elevated process, so the test is skipped where
    ///     the link cannot be created. CI runs it on Ubuntu, where no privilege is needed.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_should_replace_a_symbolic_link_planted_at_the_destination_without_following_it()
    {
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var outsidePath = Path.Join(Path.GetTempPath(), $"victim-{Guid.NewGuid():N}.txt");
        var manifestPath = Path.Join(outputPath, "SpectreDemo.json");
        const string victimContents = "must survive the export";
        var project = new ProjectItem("SpectreDemo", "Demo project", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(outsidePath, victimContents, TestContext.Current.CancellationToken);
            CreateLinkOrSkip(() => File.CreateSymbolicLink(manifestPath, outsidePath));

            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            (await File.ReadAllTextAsync(outsidePath, TestContext.Current.CancellationToken))
                .Should().Be(victimContents, "the file the planted link pointed at must not be written through the link");
            new FileInfo(manifestPath).LinkTarget.Should().BeNull("the link must have been replaced by a regular file");
            (await File.ReadAllTextAsync(manifestPath, TestContext.Current.CancellationToken)).Should().Contain("SpectreDemo");
        }
        finally
        {
            File.Delete(outsidePath);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    /// <summary>
    ///     The issue's reproduction also covers a <em>dangling</em> link: opening it with <c>O_CREAT</c> would create the
    ///     file it names, wherever that is. The replacing write must not create the link's target either.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_not_create_the_target_of_a_dangling_link_at_the_destination()
    {
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var missingTargetPath = Path.Join(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt");
        var manifestPath = Path.Join(outputPath, "SpectreDemo.json");
        var project = new ProjectItem("SpectreDemo", "Demo project", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            Directory.CreateDirectory(outputPath);
            CreateLinkOrSkip(() => File.CreateSymbolicLink(manifestPath, missingTargetPath));

            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", outputPath), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            File.Exists(missingTargetPath).Should().BeFalse("writing through a dangling link would create a file outside the export directory");
            new FileInfo(manifestPath).LinkTarget.Should().BeNull("the dangling link must have been replaced by a regular file");
        }
        finally
        {
            File.Delete(missingTargetPath);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    /// <summary>
    ///     A directory occupying the manifest's name cannot be replaced by a rename, so the export must fail cleanly:
    ///     an error result, the directory untouched, and no temporary file left behind.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_fail_without_leftovers_when_the_destination_is_a_directory()
    {
        var outputPath = Path.Join(Path.GetTempPath(), $"export-{Guid.NewGuid():N}");
        var blockingDirectory = Path.Join(outputPath, "SpectreDemo.json");
        var project = new ProjectItem("SpectreDemo", "Demo project", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            Directory.CreateDirectory(blockingDirectory);

            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", outputPath), CancellationToken.None);

            result.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
            Directory.Exists(blockingDirectory).Should().BeTrue("the existing directory must not be replaced");
            Directory.GetFiles(outputPath).Should().BeEmpty("the temporary file must be cleaned up after a failed rename");
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
    ///     A symbolic link as the output directory makes every lexical containment check describe the wrong
    ///     directory, so the export is refused rather than written to wherever the link points.
    /// </summary>
    /// <remarks>Skipped where symbolic links cannot be created, as for the destination-link test above.</remarks>
    [Fact]
    public async Task ExecuteAsync_should_reject_an_output_directory_that_is_a_symbolic_link()
    {
        var targetDirectory = Path.Join(Path.GetTempPath(), $"export-target-{Guid.NewGuid():N}");
        var linkPath = Path.Join(Path.GetTempPath(), $"export-link-{Guid.NewGuid():N}");
        var project = new ProjectItem("SpectreDemo", "Demo project", "Console", DateTime.UtcNow);
        _projectRepositoryMock.Setup(r => r.GetByNameAsync("SpectreDemo", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        try
        {
            Directory.CreateDirectory(targetDirectory);
            CreateLinkOrSkip(() => Directory.CreateSymbolicLink(linkPath, targetDirectory));

            var result = await new ExportProjectUseCase(_projectRepositoryMock.Object)
                .ExecuteAsync(new ExportProjectRequest("SpectreDemo", linkPath), CancellationToken.None);

            result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
            Directory.GetFiles(targetDirectory).Should().BeEmpty("nothing may be written through a linked output directory");
        }
        finally
        {
            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath);
            }

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
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

    private static void CreateLinkOrSkip(Action createLink)
    {
        try
        {
            createLink();
        }
        catch (Exception exception) when (OperatingSystem.IsWindows() && exception is UnauthorizedAccessException or IOException)
        {
            Assert.Skip($"Symbolic links cannot be created here (Windows needs Developer Mode or elevation): {exception.Message}");
        }
    }
}
