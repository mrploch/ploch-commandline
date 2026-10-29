using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects;

/// <summary>
///     Command demonstrating project export use case.
/// </summary>
public class ProjectExportCommand(IOutput output,
                                  ExportProjectUseCase useCase,
                                  CommandArgumentsRootProcessor settingsProcessor,
                                  ICommandSettingsValidator<ProjectExportCommandSettings> validator,
                                  IExceptionHandler exceptionHandler)
    : UseCaseAsyncCommand<ProjectExportCommandSettings, ExportProjectUseCase, ExportProjectRequest, ExportProjectResponse>(output,
                                                                                                                           useCase,
                                                                                                                           settingsProcessor,
                                                                                                                           validator,
                                                                                                                           exceptionHandler)
{
    // Opt-in settings echo, as in ProjectCreateCommand: a project name and an output path hold nothing sensitive.
    protected override bool EchoSettings => true;

    protected override ExportProjectRequest CreateRequest(ProjectExportCommandSettings commandSettings)
    {
        return new ExportProjectRequest(commandSettings.Name, commandSettings.OutputPath);
    }
}
