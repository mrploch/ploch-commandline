using System.Threading;
using System.Threading.Tasks;

namespace Ploch.Common.CommandLine
{
    public interface IAsyncCommand
    {
        Task OnExecuteAsync(CancellationToken cancellationToken = default);
    }
}