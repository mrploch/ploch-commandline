namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Request DTO for creating a project use case.
/// </summary>
public record CreateProjectRequest(string Name, string Description, string Template);

/// <summary>
///     Response DTO for creating a project use case.
/// </summary>
public record CreateProjectResponse(string Name, string Description, string Template, DateTime CreatedAt);
