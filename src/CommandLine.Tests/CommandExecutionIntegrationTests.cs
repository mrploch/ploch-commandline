using Microsoft.Extensions.Configuration;

namespace Ploch.Common.CommandLine.Tests;

public class CommandExecutionIntegrationTests
{ }

public static class TestCommandLineApp
{
    public static IDictionary<string, string> Configuration = new Dictionary<string, string>();

    public static int Main(string[] args)
    {
        return AppBuilder.CreateDefault(configuration => configuration.AddJsonFile("appsettings.json").AddInMemoryCollection(Configuration!)).Configure(container =>
        {
            //container.A
        }).Build().Execute(args);
    }
}

public class TestCommand : ICommand
{
    public void OnExecute()
    {
        throw new NotImplementedException();
    }
}