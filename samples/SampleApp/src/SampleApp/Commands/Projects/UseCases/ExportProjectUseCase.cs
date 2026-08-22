using System.Text.Json;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Use case for exporting a project: writes a manifest for the project into the requested directory.
/// </summary>
public class ExportProjectUseCase(IProjectRepository projectRepository) : IResultUseCase<ExportProjectRequest, ExportProjectResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc />
    public async Task<Result<ExportProjectResponse>> ExecuteAsync(ExportProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByNameAsync(request.Name, cancellationToken);
        if (project == null)
        {
            return Result<ExportProjectResponse>.NotFound($"Project '{request.Name}' was not found.");
        }

        // A use case that reports a successful export has to have exported something: the file is
        // written here, and an I/O failure becomes a failed Result rather than a silent success.
        string manifestPath;
        try
        {
            Directory.CreateDirectory(request.OutputPath);
            manifestPath = Path.Combine(request.OutputPath, $"{project.Name}.json");

            var manifest = JsonSerializer.Serialize(project, JsonOptions);
            await File.WriteAllTextAsync(manifestPath, manifest, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return Result<ExportProjectResponse>.Error($"Could not write the export to '{request.OutputPath}': {exception.Message}");
        }

        return Result<ExportProjectResponse>.Success(new(project.Name, manifestPath, 1, DateTime.UtcNow));
    }
}
