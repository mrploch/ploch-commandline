using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Files;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class FileProcessCommandTests
{
    private readonly Mock<ICommandSettingsValidator<FileProcessCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([new TokensArgumentsProcessor()]);

    [Fact]
    public async Task ExecuteAsync_should_expand_tokens_in_output_path()
    {
        var settings = new FileProcessCommandSettings
        {
            Path = "test.csv",
            OutputPath = "./out-{date}/test.dat"
        };

        var command = new FileProcessCommand(_processor,
                                             _validatorMock.Object,
                                             _exceptionHandlerMock.Object,
                                             _outputMock.Object);

        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "process", null);

        var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        settings.OutputPath.Should().NotContain("{date}");
        settings.OutputPath.Should().MatchRegex(@"\./out-\d{4}-\d{2}-\d{2}/test\.dat");
    }

    [Fact]
    public async Task ExecuteAsync_should_report_cancelled_when_the_token_is_already_cancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var settings = new FileProcessCommandSettings { Path = "test.csv", OutputPath = "./out/test.dat" };

        var command = new FileProcessCommand(_processor, _validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object);

        var result = await command.ExecuteAsync(new([], Mock.Of<IRemainingArguments>(), "process", null), settings, cancellationTokenSource.Token);

        result.Should().Be((int)ExitCode.Cancelled);
        _exceptionHandlerMock.Verify(h => h.HandleException(It.IsAny<Exception>()), Times.Never);
    }
}
