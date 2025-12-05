using System.Threading.Tasks;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.Common.Reflection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.UseCases;

public abstract class UseCaseAsyncCommand<TCommandSettings, TUseCase, TUseCaseRequest, TUseCaseResponse>(
    IOutput output,
    TUseCase useCase,
    CommandArgumentsRootProcessor settingsProcessor,
    ICommandSettingsValidator<TCommandSettings> validator,
    IExceptionHandler exceptionHandler) : AsyncAppCommand<TCommandSettings>(settingsProcessor, validator, exceptionHandler)
    where TCommandSettings : CommandSettings where TUseCase : IResultUseCase<TUseCaseRequest, TUseCaseResponse>
{
    protected TUseCase UseCase => useCase;

    protected abstract TUseCaseRequest CreateRequest(TCommandSettings commandSettings);

    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, TCommandSettings settings)
    {
        output.MarkupLineInterpolated($"[underline]Starting use case {typeof(TUseCase).Name}[/]");
        output.MarkupLineInterpolated($"[dim]Settings:[/]");
        var propertyValues = settings.GetPropertyValues();
        foreach (var (propertyName, propertyValue) in propertyValues)
        {
            output.MarkupLineInterpolated($"[dim]{propertyName}[/]: {propertyValue}");
        }

        var request = CreateRequest(settings);

        var response = await UseCase.ExecuteAsync(request);

        if (response.IsSuccess)
        {
            return ProcessSuccessResponse(response);
        }

        return ProcessFailureResponse(response);
    }

    protected virtual ExitCode ProcessFailureResponse(Result<TUseCaseResponse> result)
    {
        output.MarkupLineInterpolated($"[red]Use case failed: {string.Join(", ", result.Errors)}[/]");

        return ExitCode.Error;
    }

    protected virtual ExitCode ProcessSuccessResponse(Result<TUseCaseResponse> result)
    {
        output.MarkupLineInterpolated($"[green]Use case completed successfully.[/]");

        return ExitCode.Success;
    }
}
