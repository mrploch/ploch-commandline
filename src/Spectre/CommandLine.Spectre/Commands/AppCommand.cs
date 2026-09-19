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
///     work around <see cref="DoExecute" />: it prints the execution banner, runs the settings processor, and routes
///     exceptions to the <see cref="IExceptionHandler" />.
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
    /// <remarks>
    ///     The settings are passed through the configured settings processor before <see cref="DoExecute" /> runs.
    ///     Exceptions raised while writing the banner, by the settings processor or by <see cref="DoExecute" /> do not propagate: they are passed to the configured
    ///     <see cref="IExceptionHandler" />, whose result becomes the exit code. An <see cref="OperationCanceledException" /> is the
    ///     exception to that rule: it is treated as a requested outcome and returns <see cref="ExitCode.Cancelled" /> without reaching
    ///     the handler.
    /// </remarks>
    public override int Execute(CommandContext context, TSettings settings, CancellationToken cancellationToken)
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

            return (int)DoExecute(context, settings, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a requested outcome, not a fault: it must not be routed to the exception handler.
            return (int)ExitCode.Cancelled;
        }
        catch (Exception ex)
        {
            return exceptionHandler.HandleException(ex);
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
