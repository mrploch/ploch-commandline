using System.Diagnostics.CodeAnalysis;

namespace Ploch.Common.CommandLine;

/// <summary>
/// Defines the contract for a command that can be executed by the command-line application.
/// </summary>
/// <remarks>
/// Implement this interface to create a command that can be executed from the command line.
/// The <see cref="OnExecute"/> method contains the command's main logic.
/// </remarks>
public interface ICommand
{
    /// <summary>
    /// Executes the command with the specified parameters.
    /// </summary>
    /// <remarks>
    /// This method is called when the command is invoked from the command line.
    /// Implement this method to define the command's behavior.
    /// </remarks>
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Called dynamically by the CommandLineUtils library")]
    void OnExecute();
}