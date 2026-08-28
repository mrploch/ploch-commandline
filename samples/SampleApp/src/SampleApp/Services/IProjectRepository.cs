using Ploch.CommandLine.Spectre.SampleApp.Services.Models;

namespace Ploch.CommandLine.Spectre.SampleApp.Services;

/// <summary>
///     Repository interface for sample projects.
/// </summary>
public interface IProjectRepository
{
    Task<ProjectItem?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(ProjectItem project, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProjectItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
