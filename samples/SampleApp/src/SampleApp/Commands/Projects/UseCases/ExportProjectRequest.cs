namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Request DTO for exporting a project use case.
/// </summary>
public record ExportProjectRequest(string Name, string OutputPath);

/// <summary>
///     Response DTO for exporting a project use case.
/// </summary>
public record ExportProjectResponse(string Name, string OutputPath, int ItemCount, DateTime ExportedAt);
