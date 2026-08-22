using Ardalis.Result;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Use case for creating a project, demonstrating <see cref="IResultUseCase{TRequest, TResponse}" /> with Ardalis.Result.
/// </summary>
public class CreateProjectUseCase(IProjectRepository projectRepository) : IResultUseCase<CreateProjectRequest, CreateProjectResponse>
{
    public async Task<Result<CreateProjectResponse>> ExecuteAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<CreateProjectResponse>.Error("Project name cannot be empty.");
        }

        var existing = await projectRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            return Result<CreateProjectResponse>.Conflict($"A project with name '{request.Name}' already exists.");
        }

        var project = new ProjectItem(request.Name, request.Description, request.Template, DateTime.UtcNow);
        await projectRepository.AddAsync(project, cancellationToken);

        var response = new CreateProjectResponse(project.Name, project.Description, project.Template, project.CreatedAt);

        return Result<CreateProjectResponse>.Success(response);
    }
}
