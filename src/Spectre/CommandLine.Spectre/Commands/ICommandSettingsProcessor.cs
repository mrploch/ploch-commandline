using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Defines a contract for processing command settings before a command is executed.
/// </summary>
public interface ICommandSettingsProcessor
{
    /// <summary>
    ///     Processes the supplied command settings, applying any transformations the processor provides.
    /// </summary>
    /// <param name="arguments">The command settings to process.</param>
    void ProcessArguments(CommandSettings arguments);
}
