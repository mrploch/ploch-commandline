using Ardalis.Result;

namespace Ploch.CommandLine.UseCases;

/// <summary>
///     Defines a use case that operates on a request and returns a result object from the Ardalis.Result library.
/// </summary>
/// <typeparam name="TRequest">The type of the request object that this use case handles.</typeparam>
/// <typeparam name="TResponse">The type of the value returned in a successful <see cref="Result{T}" />.</typeparam>
public interface IResultUseCase<in TRequest, TResponse> : IUseCase<TRequest, Result<TResponse>>
{
    /// <summary>
    ///     Gets the name of the use case, typically derived from the implementing class's name.
    /// </summary>
    /// <value>The name of the use case.</value>
    string? UseCaseName => GetType().Name;
}
