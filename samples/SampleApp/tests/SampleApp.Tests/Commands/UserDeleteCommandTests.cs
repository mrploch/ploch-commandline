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

        var result = await CreateCommand().ExecuteAsync(CreateContext(),
                                                        new UserDeleteCommandSettings { Id = 7, Force = true },
                                                        cancellationTokenSource.Token);

        result.Should().Be((int)ExitCode.Success);
        _userServiceMock.Verify(s => s.DeleteUserAsync(7, cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_should_return_error_when_the_user_does_not_exist()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateCommand().ExecuteAsync(CreateContext(),
                                                        new UserDeleteCommandSettings { Id = 99, Force = true },
                                                        CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
    }

    [Fact]
    public async Task ExecuteAsync_should_refuse_to_delete_without_force_when_the_console_cannot_confirm()
    {
        // The test host has no interactive console, which is the same situation as a CI pipeline:
        // the command must refuse rather than delete unprompted.
        var result = await CreateCommand().ExecuteAsync(CreateContext(), new UserDeleteCommandSettings { Id = 7 }, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
        _userServiceMock.Verify(s => s.DeleteUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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
