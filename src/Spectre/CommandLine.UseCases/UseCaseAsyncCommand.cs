using System.Linq;
using System.Threading.Tasks;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.Common.Reflection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.UseCases;

/// <summary>
///     Base class for asynchronous commands that delegate their work to a use case and render its result.
/// </summary>
/// <typeparam name="TCommandSettings">The settings type accepted by the command.</typeparam>
/// <typeparam name="TUseCase">The use case type executed by the command.</typeparam>
/// <typeparam name="TUseCaseRequest">The request type passed to the use case.</typeparam>
/// <typeparam name="TUseCaseResponse">The response type produced by the use case.</typeparam>
/// <param name="output">The output writer used to render progress and results.</param>
/// <param name="useCase">The use case executed by this command.</param>
/// <param name="settingsProcessor">The processor applied to the command settings before execution.</param>
/// <param name="validator">The validator used to validate the command settings.</param>
/// <param name="exceptionHandler">The handler used to process exceptions raised during execution.</param>
public abstract class UseCaseAsyncCommand<TCommandSettings, TUseCase, TUseCaseRequest, TUseCaseResponse>(
    IOutput output,
    TUseCase useCase,
    CommandArgumentsRootProcessor settingsProcessor,
    ICommandSettingsValidator<TCommandSettings> validator,
    IExceptionHandler exceptionHandler) : AsyncAppCommand<TCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
    where TCommandSettings : CommandSettings where TUseCase : IResultUseCase<TUseCaseRequest, TUseCaseResponse>
{
    /// <summary>
    ///     Gets the use case executed by this command.
    /// </summary>
    protected TUseCase UseCase => useCase;

    /// <summary>
    ///     Gets a value indicating whether every public settings value is echoed to the output before the use case
    ///     runs. Defaults to <see langword="false" />.
    /// </summary>
    /// <remarks>
    ///     This is opt-in because the echo is indiscriminate: it prints every public property of the settings type,
    ///     and a derived command is free to add a password, an API token or a connection string as an option. Since
    ///     this is a base class in a library, a consumer would not have chosen to disclose those values, and console
    ///     output is routinely captured by CI logs. Override this to return <see langword="true" /> on a command
    ///     whose settings are known to carry nothing sensitive.
    /// </remarks>
    protected virtual bool EchoSettings => false;

    /// <summary>
    ///     Builds the use case request from the validated command settings.
    /// </summary>
    /// <param name="commandSettings">The settings supplied on the command line.</param>
    /// <returns>The request to pass to the use case.</returns>
    protected abstract TUseCaseRequest CreateRequest(TCommandSettings commandSettings);

    /// <summary>
    ///     Executes the use case, echoing the supplied settings first and then rendering the outcome.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token forwarded to the use case so it can honour cancellation.</param>
    /// <returns>
    ///     The result of <see cref="ProcessSuccessResponse" /> when the use case succeeds; otherwise the result of
    ///     <see cref="ProcessFailureResponse" />.
    /// </returns>
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context, TCommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"Starting use case [underline]{UseCase.UseCaseName}[/]");

        if (EchoSettings)
        {
            Output.WriteLine("[dim]Settings:[/]");
            var propertyValues = settings.GetPropertyValues();
            foreach (var (propertyName, propertyValue) in propertyValues)
            {
                Output.MarkupLineInterpolated($"[dim]{propertyName}[/]: {propertyValue}");
            }
        }

        var request = CreateRequest(settings);

        var response = await UseCase.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccess)
        {
            return ProcessSuccessResponse(response);
        }

        return ProcessFailureResponse(response);
    }

    /// <summary>
    ///     Renders a failed use case result. Override to customise failure reporting.
    /// </summary>
    /// <param name="result">The failed result returned by the use case.</param>
    /// <returns><see cref="ExitCode.Error" />.</returns>
    /// <remarks>
    ///     Both message collections are rendered. <c>Result.Error</c> fills <c>Errors</c>, while
    ///     <c>Result.Invalid</c> fills <c>ValidationErrors</c> and leaves <c>Errors</c> empty - so reading only one
    ///     of them makes a whole class of failure arrive on the console with no explanation at all. When both
    ///     collections are empty - or carry nothing but whitespace - the status is shown, so the reason after the
    ///     label is never blank.
    /// </remarks>
    protected virtual ExitCode ProcessFailureResponse(Result<TUseCaseResponse> result)
    {
        var messages = result.Errors
                             .Concat(result.ValidationErrors.Select(DescribeValidationError))
                             .Where(message => !string.IsNullOrWhiteSpace(message))
                             .ToArray();

        var detail = messages.Length == 0 ? result.Status.ToString() : string.Join(", ", messages);

        Output.MarkupLineInterpolated($"[red]Use case failed: {detail}[/]");

        return ExitCode.Error;
    }

    /// <summary>
    ///     Renders a successful use case result. Override to customise success reporting.
    /// </summary>
    /// <param name="result">The successful result returned by the use case.</param>
    /// <returns><see cref="ExitCode.Success" />.</returns>
    protected virtual ExitCode ProcessSuccessResponse(Result<TUseCaseResponse> result)
    {
        Output.WriteLine("[green]Use case completed successfully.[/]");

        return ExitCode.Success;
    }

    /// <summary>
    ///     Describes a validation error for the console.
    /// </summary>
    /// <param name="validationError">The validation error to describe.</param>
    /// <returns>The error message, or the identifier when the error carries no message.</returns>
    /// <remarks>
    ///     A <see cref="ValidationError" /> is not obliged to carry an <c>ErrorMessage</c>. Falling back to the
    ///     identifier still names the offending field, which is more use than dropping the error and reporting the
    ///     bare status.
    /// </remarks>
    private static string DescribeValidationError(ValidationError validationError) =>
        string.IsNullOrWhiteSpace(validationError.ErrorMessage) ? validationError.Identifier : validationError.ErrorMessage;
}
