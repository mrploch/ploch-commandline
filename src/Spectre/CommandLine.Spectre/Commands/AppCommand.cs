using Ploch.CommandLine.Spectre.Output;
using Ploch.Common.ArgumentChecking;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Base class for application commands that provides common functionality for command execution and validation.
/// </summary>
/// <typeparam name="TSettings">The type of settings used by the command.</typeparam>
/// <param name="settingsProcessor">The processor applied to the command settings before execution.</param>
/// <param name="validator">The validator used to validate command settings.</param>
/// <param name="exceptionHandler">The handler used to process exceptions that occur during command execution.</param>
/// <param name="output">The output writer used to render command output.</param>
/// <remarks>
///     This is the synchronous counterpart of <see cref="AsyncAppCommand{TSettings}" /> and performs the same framework
///     work around <see cref="DoExecute" />: it runs the settings processor and routes exceptions to the
///     <see cref="IExceptionHandler" />. It writes nothing to <paramref name="output" /> itself; the "Executing command"
///     line that announces the command is written to standard error by the application before the command runs.
/// </remarks>
public abstract class AppCommand<TSettings>(CommandArgumentsRootProcessor settingsProcessor,
                                            ICommandSettingsValidator<TSettings> validator,
                                            IExceptionHandler exceptionHandler,
                                            IOutput output) : Command<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    ///     Gets the output writer used to render command output.
    /// </summary>
    protected IOutput Output => output;

    /// <summary>
    ///     Executes the command with the specified context and settings.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token that is forwarded to <see cref="DoExecute" /> so implementations can honour cancellation.</param>
    /// <returns>An integer representing the exit code of the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or settings is null.</exception>
    /// <exception cref="ExceptionHandlerFailedException">
    ///     Thrown when the configured <see cref="IExceptionHandler" /> itself throws while handling an exception; the
    ///     handler's exception is its <see cref="Exception.InnerException" /> and the exception being handled is
    ///     <see cref="ExceptionHandlerFailedException.Original" />.
    /// </exception>
    /// <remarks>
    ///     The settings are passed through the configured settings processor before <see cref="DoExecute" /> runs.
    ///     Exceptions raised by the settings processor or by <see cref="DoExecute" /> do not propagate: they are passed to the configured
    ///     <see cref="IExceptionHandler" />, whose result becomes the exit code; a handler that throws surfaces as
    ///     <see cref="ExceptionHandlerFailedException" />, which the application's Spectre handler reports once and turns into
    ///     <see cref="ExitCode.Error" />. An <see cref="OperationCanceledException" /> thrown while
    ///     <paramref name="cancellationToken" /> has cancellation requested is the exception to that rule: it is treated as the requested
    ///     outcome and returns <see cref="ExitCode.Cancelled" /> without reaching the handler. An <see cref="OperationCanceledException" />
    ///     raised while that token is still live - a <see cref="TaskCanceledException" /> from an <c>HttpClient</c> timeout, or an inner
    ///     token the implementation created and cancelled itself - is a fault nobody asked for and goes to the handler like any other
    ///     exception.
    /// </remarks>
    public override int Execute(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        context.NotNull();
        settings.NotNull();

        try
        {
            settingsProcessor.ProcessArguments(settings);

            return (int)DoExecute(context, settings, cancellationToken);
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
    /// <returns>A validation result indicating whether the settings are valid.</returns>
    public override ValidationResult Validate(CommandContext context, TSettings settings) => validator.Validate(context, settings);

    /// <summary>
    ///     Implements the command's core execution logic.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token that signals the command should stop work.</param>
    /// <returns>An exit code indicating the result of the command execution.</returns>
    protected abstract ExitCode DoExecute(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
