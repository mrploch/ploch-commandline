using McMaster.Extensions.CommandLineUtils;

namespace Ploch.Common.CommandLine.Tests;

[Command(Name = "testCommand")]
public class TestCommand(TestCallback testCallback) : IAsyncCommand
{
    [Option] public string? TestArg { get; set; }

    public Task OnExecuteAsync(CancellationToken cancellationToken = default)
    {
        testCallback.Execute(TestArg);

        return Task.CompletedTask;
    }
}