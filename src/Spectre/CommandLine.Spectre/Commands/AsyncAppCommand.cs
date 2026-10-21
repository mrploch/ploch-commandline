using Ploch.CommandLine.Spectre.Output;
using Spectre.Console;
using Spectre.Console.Cli;
#pragma warning disable S1128 // Unnecessary "using" should be removed - for some reason it reports a used entry (.NotNull() etc.) - I should disable this rule if it gives more false positives
using Ploch.Common.ArgumentChecking;

#pragma warning restore S1128

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Base class for asynchronous commands that provides common functionality for command execution and validation.
/// </summary>
/// <typeparam name="TSettings">The type of settings the command uses.</typeparam>
/// <param name="settingsProcessor">The processor applied to the command settings before execution.</param>
/// <param name="validator">The validator used to validate command settings.</param>
/// <param name="exceptionHandler">The handler used to process exceptions that occur during command execution.</param>
/// <param name="output">The output writer used to render command output.</param>
public abstract class AsyncAppCommand<TSettings>(CommandArgumentsRootProcessor settingsProcessor,
                                                 ICommandSettingsValidator<TSettings> validator,
                                                 IExceptionHandler exceptionHandler,
                                                 IOutput output) : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    /// <summary>
    ///     Gets the output writer used to render command output.
    /// </summary>
    protected IOutput Output => output;

    /// <summary>
    ///     Executes the command asynchronously with the provided context and settings.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token that is forwarded to <see cref="DoExecuteAsync" /> so implementations can honour cancellation.</param>
    /// <returns>An integer representing the exit code of the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or settings is null.</exception>
    /// <exception cref="ExceptionHandlerFailedException">
    ///     Thrown when the configured <see cref="IExceptionHandler" /> itself throws while handling an exception; the
    ///     handler's exception is its <see cref="Exception.InnerException" /> and the exception being handled is
    ///     <see cref="ExceptionHandlerFailedException.Original" />.
    /// </exception>
    /// <remarks>
    ///     The settings are passed through the configured settings processor before <see cref="DoExecuteAsync" /> runs.
    ///     Exceptions raised while writing the banner, by the settings processor or by <see cref="DoExecuteAsync" /> do not propagate: they are passed to the configured
    ///     <see cref="IExceptionHandler" />, whose result becomes the exit code; a handler that throws surfaces as
    ///     <see cref="ExceptionHandlerFailedException" />, which the application's Spectre handler reports once and turns into
    ///     <see cref="ExitCode.Error" />. An <see cref="OperationCanceledException" /> thrown while
    ///     <paramref name="cancellationToken" /> has cancellation requested is the exception to that rule: it is treated as the requested
    ///     outcome and returns <see cref="ExitCode.Cancelled" /> without reaching the handler. An <see cref="OperationCanceledException" />
    ///     raised while that token is still live - a <see cref="TaskCanceledException" /> from an <c>HttpClient</c> timeout, or an inner
    ///     token the implementation created and cancelled itself - is a fault nobody asked for and goes to the handler like any other
    ///     exception.
    /// </remarks>
    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        context.NotNull();
        settings.NotNull();

        try
        {
            // Inside the try: a failing output (for example a broken console stream) is a fault like any other and
            // must reach the exception handler rather than escape the command.
            output.MarkupLineInterpolated($"Executing command [bold underline]{settings.GetType().Name}[/]");
            output.WriteLine();
            output.WriteLine("Processing arguments...");

            settingsProcessor.ProcessArguments(settings);

            return (int)await DoExecuteAsync(context, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is a requested outcome, not a fault, but only when it was requested through the token this
            // command received. An OperationCanceledException raised while that token is live - an HttpClient timeout,
            // an inner token the implementation cancelled itself - is a fault nobody asked for, so the filter lets it
            // fall through to the exception handler rather than reporting it as "the user cancelled" (PLO-756).
            return (int)ExitCode.Cancelled;
        }
        catch (Exception ex)
        {
            // Through the wrapper so a handler that throws is reported once by the application's Spectre handler
            // rather than asked to handle its own exception (PLO-782).
            return ExceptionHandlerInvoker.Invoke(exceptionHandler, ex);
        }
    }

    /// <summary>
    ///     Validates the command settings using the provided validator.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to validate.</param>
    /// <returns>A ValidationResult indicating whether the settings are valid.</returns>
    public override ValidationResult Validate(CommandContext context, TSettings settings) => validator.Validate(context, settings);

    /// <summary>
    ///     Implements the command's execution logic.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token that signals the command should stop work.</param>
    /// <returns>A task representing the asynchronous operation that returns an ExitCode.</returns>
    protected abstract Task<ExitCode> DoExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
