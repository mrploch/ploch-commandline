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

        // The project name reaches this method from a command argument and is about to become a path
        // segment. Left unchecked, a name such as "../outside" turns an export requested for "./exports"
        // into a write to "./outside.json" - outside the directory the caller asked for.
        var fileName = $"{project.Name}.json";
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return Result<ExportProjectResponse>.Invalid(new ValidationError
                                                         {
                                                             Identifier = nameof(request.Name),
                                                             ErrorMessage =
                                                                 $"Project name '{project.Name}' cannot be used as a file name. Names must not contain path separators or other characters that are invalid in a file name."
                                                         });
        }

        // A use case that reports a successful export has to have exported something: the file is
        // written here, and an I/O failure becomes a failed Result rather than a silent success.
        string manifestPath;
        try
        {
            Directory.CreateDirectory(request.OutputPath);
            // Path.GetFileName is a no-op given the guard above, which already rejects any name carrying a
            // separator. It is kept because this is the one path where a caller-supplied name becomes a file
            // path: it makes "this argument is a bare file name" a local invariant rather than one the reader
            // has to go and re-derive, and it removes Path.Combine's rooted-argument footgun outright.
            manifestPath = Path.Combine(request.OutputPath, Path.GetFileName(fileName));

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
