using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public interface ICommandSettingsProcessor
{
    void ProcessArguments(CommandSettings arguments);
}
