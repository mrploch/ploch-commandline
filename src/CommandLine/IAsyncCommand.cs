using System.Threading;
using System.Threading.Tasks;

namespace Ploch.Common.CommandLine;

/// <summary>
/// Defines the contract for an asynchronous command that can be executed by the command-line application.
/// </summary>
/// <remarks>
/// Implement this interface to create an asynchronous command that can be executed from the command line.
/// The <see cref="OnExecuteAsync"/> method contains the command's main logic and supports cancellation.
/// </remarks>
public interface IAsyncCommand
{
    /// <summary>
    /// Asynchronously executes the command with the specified cancellation token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called when the command is invoked from the command line.
    /// Implement this method to define the command's asynchronous behavior.
    /// The cancellation token should be monitored for cancellation requests.
    /// </remarks>
    Task OnExecuteAsync(CancellationToken cancellationToken = default);
}