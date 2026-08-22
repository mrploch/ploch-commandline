using Ardalis.Result;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Use case for exporting a project.
/// </summary>
public class ExportProjectUseCase(IProjectRepository projectRepository) : IResultUseCase<ExportProjectRequest, ExportProjectResponse>
{
    public async Task<Result<ExportProjectResponse>> ExecuteAsync(ExportProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByNameAsync(request.Name, cancellationToken);
        if (project == null)
        {
            return Result<ExportProjectResponse>.NotFound($"Project '{request.Name}' was not found.");
        }

        var response = new ExportProjectResponse(project.Name, request.OutputPath, 1, DateTime.UtcNow);

        return Result<ExportProjectResponse>.Success(response);
    }
}
