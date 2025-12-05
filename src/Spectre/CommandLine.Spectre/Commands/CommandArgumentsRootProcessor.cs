using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public class CommandArgumentsRootProcessor(IEnumerable<ICommandSettingsProcessor> processors) : ICommandSettingsProcessor
{
    public void ProcessArguments(CommandSettings arguments)
    {
        foreach (var propertiesProcessor in processors)
        {
            propertiesProcessor.ProcessArguments(arguments);
        }
    }
}
