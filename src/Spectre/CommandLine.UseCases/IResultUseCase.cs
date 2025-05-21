using Ardalis.Result;

namespace Ploch.CommandLine.UseCases;

public interface IResultUseCase<TRequest, TResponse> : IUseCase<TRequest, Result<TResponse>>
{ }
