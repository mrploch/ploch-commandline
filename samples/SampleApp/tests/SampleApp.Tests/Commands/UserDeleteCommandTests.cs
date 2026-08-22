using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class UserDeleteCommandTests
{
    private readonly Mock<ICommandSettingsValidator<UserDeleteCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_return_success_when_the_user_was_deleted()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        _userServiceMock.Setup(s => s.DeleteUserAsync(7, cancellationTokenSource.Token)).ReturnsAsync(true);

        var result = await CreateCommand().ExecuteAsync(CreateContext(), new UserDeleteCommandSettings { Id = 7 }, cancellationTokenSource.Token);

        result.Should().Be((int)ExitCode.Success);
        _userServiceMock.Verify(s => s.DeleteUserAsync(7, cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_should_return_error_when_the_user_does_not_exist()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateCommand().ExecuteAsync(CreateContext(), new UserDeleteCommandSettings { Id = 99 }, CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "delete", null);

    private UserDeleteCommand CreateCommand() =>
        new(_processor,
            _validatorMock.Object,
            _exceptionHandlerMock.Object,
            _outputMock.Object,
            _userServiceMock.Object,
            NullLogger<UserDeleteCommand>.Instance);
}
