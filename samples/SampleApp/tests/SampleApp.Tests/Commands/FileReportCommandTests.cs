using FluentAssertions;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Files;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

public class FileReportCommandTests
{
    private readonly Mock<ICommandSettingsValidator<FileReportCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_return_invalid_input_when_the_file_does_not_exist()
    {
        var settings = new FileReportCommandSettings { Path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv") };

        var result = await CreateCommand().ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
    }

    [Fact]
    public async Task ExecuteAsync_should_return_success_for_an_existing_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, "id,name", TestContext.Current.CancellationToken);

        try
        {
            var result = await CreateCommand().ExecuteAsync(CreateContext(), new FileReportCommandSettings { Path = path }, CancellationToken.None);

            result.Should().Be((int)ExitCode.Success);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "report", null);

    private FileReportCommand CreateCommand() =>
        new(_processor, _validatorMock.Object, _exceptionHandlerMock.Object, _outputMock.Object);
}
