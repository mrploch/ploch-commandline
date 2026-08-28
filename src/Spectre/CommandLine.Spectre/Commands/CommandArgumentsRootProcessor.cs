using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Applies every registered <see cref="ICommandSettingsProcessor" /> to a set of command settings, in registration order.
/// </summary>
/// <param name="processors">The processors to apply.</param>
public class CommandArgumentsRootProcessor(IEnumerable<ICommandSettingsProcessor> processors) : ICommandSettingsProcessor
{
    /// <summary>
    ///     Passes the supplied settings through each registered processor in turn.
    /// </summary>
    /// <param name="arguments">The command settings to process.</param>
    public void ProcessArguments(CommandSettings arguments)
    {
        foreach (var propertiesProcessor in processors)
        {
            propertiesProcessor.ProcessArguments(arguments);
        }
    }
}
