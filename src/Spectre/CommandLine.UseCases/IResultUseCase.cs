using Ardalis.Result;

namespace Ploch.Tools.SystemProfiles.UseCases;

public interface IResultUseCase<TRequest, TResponse> : IUseCase<TRequest, Result<TResponse>>
{ }
