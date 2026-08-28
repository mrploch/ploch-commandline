using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class UserAddCommandTests
{
    private readonly Mock<ICommandSettingsValidator<UserAddCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_create_user_and_return_success()
    {
        var settings = new UserAddCommandSettings
        {
            Name = "John Doe",
            Email = "john@example.com",
            Role = "Developer"
        };

        var createdUser = new UserProfile(10, "John Doe", "john@example.com", "Developer", true, DateTime.UtcNow);
        _userServiceMock.Setup(s => s.CreateUserAsync(settings.Name, settings.Email, settings.Role, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(createdUser);

        var command = new UserAddCommand(_processor,
                                         _validatorMock.Object,
                                         _exceptionHandlerMock.Object,
                                         _outputMock.Object,
                                         _userServiceMock.Object);

        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "add", null);

        var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        _userServiceMock.Verify(s => s.CreateUserAsync("John Doe", "john@example.com", "Developer", It.IsAny<CancellationToken>()), Times.Once);
    }
}
