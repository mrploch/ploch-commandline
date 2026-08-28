using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class ProjectCreateCommandTests
{
    private readonly Mock<IOutput> _outputMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ICommandSettingsValidator<ProjectCreateCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_execute_use_case_and_return_success()
    {
        var settings = new ProjectCreateCommandSettings
        {
            Name = "NewApp",
            Description = "New application description",
            Template = "Console"
        };

        var useCase = new CreateProjectUseCase(_projectRepositoryMock.Object);
        var command = new ProjectCreateCommand(_outputMock.Object,
                                             useCase,
                                             _processor,
                                             _validatorMock.Object,
                                             _exceptionHandlerMock.Object);

        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "create", null);

        var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<Services.Models.ProjectItem>(p => p.Name == "NewApp"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
