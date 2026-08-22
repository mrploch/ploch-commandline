using System.Collections.Concurrent;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;

namespace Ploch.CommandLine.Spectre.SampleApp.Services;

/// <summary>
///     In-memory implementation of <see cref="IProjectRepository" />.
/// </summary>
public class InMemoryProjectRepository : IProjectRepository
{
    private readonly ConcurrentDictionary<string, ProjectItem> _projects = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryProjectRepository()
    {
        _projects["SpectreDemo"] = new ProjectItem("SpectreDemo", "Demo project for Spectre Console CLI", "Console", DateTime.UtcNow.AddDays(-10));
        _projects["WebBackend"] = new ProjectItem("WebBackend", "API backend project", "WebAPI", DateTime.UtcNow.AddDays(-20));
    }

    public Task<ProjectItem?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _projects.TryGetValue(name, out var project);

        return Task.FromResult(project);
    }

    public Task AddAsync(ProjectItem project, CancellationToken cancellationToken = default)
    {
        _projects[project.Name] = project;

        return Task.CompletedTask;
    }

    public Task<IEnumerable<ProjectItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ProjectItem>>(_projects.Values.ToList());
    }
}
