using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine.Tests;

public static class TestCommandLineApp
{
    public static IDictionary<string, string> Configuration = new Dictionary<string, string>();

    public static int AppMain(string[] args, TestCallback testCallback)
    {
        return AppBuilder.CreateDefault(new CommandAppProperties("Test App", "My Test App"), configuration => configuration.AddInMemoryCollection(Configuration!))
            .Configure(appContainer =>
                       {
                           appContainer.Services.AddSingleton(testCallback)
                               .AddSingleton<TestCommand>()
                               .AddSingleton<IAsyncCommand, TestCommand>();

                           appContainer.Application.Command<TestCommand>();
                       })
            .BuildConsole()
            .Execute(args);
    }
}