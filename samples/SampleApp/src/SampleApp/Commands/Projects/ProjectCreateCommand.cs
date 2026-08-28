using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects;

/// <summary>
///     Command demonstrating <see cref="UseCaseAsyncCommand{TCommandSettings, TUseCase, TUseCaseRequest, TUseCaseResponse}" />.
///     Integrates cleanly with Clean Architecture use cases and Ardalis.Result.
/// </summary>
public class ProjectCreateCommand(IOutput output,
                                  CreateProjectUseCase useCase,
                                  CommandArgumentsRootProcessor settingsProcessor,
                                  ICommandSettingsValidator<ProjectCreateCommandSettings> validator,
                                  IExceptionHandler exceptionHandler)
    : UseCaseAsyncCommand<ProjectCreateCommandSettings, CreateProjectUseCase, CreateProjectRequest, CreateProjectResponse>(output,
                                                                                                                           useCase,
                                                                                                                           settingsProcessor,
                                                                                                                           validator,
                                                                                                                           exceptionHandler)
{
    protected override CreateProjectRequest CreateRequest(ProjectCreateCommandSettings commandSettings)
    {
        return new CreateProjectRequest(commandSettings.Name, commandSettings.Description, commandSettings.Template);
    }
}
