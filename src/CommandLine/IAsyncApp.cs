using System.Threading.Tasks;

namespace Ploch.Common.CommandLine
{
    public interface IAsyncApp
    {
        Task OnExecuteAsync();
    }
}