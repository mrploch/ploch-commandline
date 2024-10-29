using McMaster.Extensions.CommandLineUtils;

namespace Ploch.Common.CommandLine;

public abstract class HelpOnlyCommand(CommandLineApplication app) : ICommand
{
    public virtual void OnExecute()
    {
        app.WriteHelpText();
    }
}