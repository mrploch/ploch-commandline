using System.Threading;
using System.Threading.Tasks;

namespace Ploch.Tools.SystemProfiles.UseCases;

/// <summary>
///     Defines a use case with a request and response.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IUseCase<in TRequest, TResponse>
{
    /// <summary>
    ///     Executes the use case asynchronously with the specified request and cancellation token.
    /// </summary>
    /// <param name="request">The request object containing the necessary data for the use case.</param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None" />.
    /// </param>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <typeparam name="TResponse">The type of the response object.</typeparam>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the response of type
    ///     <typeparamref name="TResponse" />.
    /// </returns>
    public Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}