using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Ploch.Common.CommandLine;

public interface ICommand
{
    /// <summary>
    ///     Executes the command.
    /// </summary
    /// <returns>Executed status code integer</returns>
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Called dynamically by the CommandLineUtils library")]
    void OnExecute();
}

public interface IAsyncCommand
{
    /// <summary>
    ///     Executes the command.
    /// </summary
    /// <returns>Executed status code integer</returns>
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Called dynamically by the CommandLineUtils library")]
    Task OnExecuteAsync();
}