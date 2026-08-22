using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class UserListCommandTests
{
    private readonly Mock<ICommandSettingsValidator<UserListCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_return_invalid_input_when_the_format_is_not_supported()
    {
        var settings = new UserListCommandSettings { Format = "xml" };

        var result = await CreateCommand().ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
        _userServiceMock.Verify(s => s.GetUsersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_should_forward_the_cancellation_token_to_the_user_service()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var settings = new UserListCommandSettings { Format = "compact" };
        _userServiceMock.Setup(s => s.GetUsersAsync(false, cancellationTokenSource.Token))
                        .ReturnsAsync([new UserProfile(1, "Alice Smith", "alice@example.com", "Developer", true, DateTime.UtcNow)]);

        var result = await CreateCommand().ExecuteAsync(CreateContext(), settings, cancellationTokenSource.Token);

        result.Should().Be((int)ExitCode.Success);
        _userServiceMock.Verify(s => s.GetUsersAsync(false, cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_should_report_success_when_no_users_match()
    {
        var settings = new UserListCommandSettings { ActiveOnly = true };
        _userServiceMock.Setup(s => s.GetUsersAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateCommand().ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "list", null);

    private UserListCommand CreateCommand() =>
        new(_processor, _validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object, _userServiceMock.Object);
}
