using System.Threading.Tasks;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.UseCases;

public abstract class UseCaseAsyncCommand<TCommandSettings, TUseCase, TUseCaseRequest, TUseCaseResponse>(
    TUseCase useCase,
    ICommandSettingsValidator<TCommandSettings> validator,
    IExceptionHandler<UseCaseAsyncCommand<TCommandSettings, TUseCase, TUseCaseRequest, TUseCaseResponse>> exceptionHandler)
    : AsyncAppCommand<TCommandSettings>(validator, exceptionHandler)
    where TCommandSettings : CommandSettings where TUseCase : IResultUseCase<TUseCaseRequest, TUseCaseResponse>
{
    protected TUseCase UseCase => useCase;

    protected abstract TUseCaseRequest CreateRequest(TCommandSettings commandSettings);

    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, TCommandSettings settings)
    {
        var request = CreateRequest(settings);

        var response = await UseCase.ExecuteAsync(request);

        if (response.IsSuccess)
        {
            return ProcessSuccessResponse(response);
        }

        return ProcessFailureResponse(response);
    }

    protected virtual ExitCode ProcessFailureResponse(Result<TUseCaseResponse> result) => ExitCode.Error;

    protected virtual ExitCode ProcessSuccessResponse(Result<TUseCaseResponse> result) => ExitCode.Success;
}
