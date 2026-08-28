using Ploch.Common.ArgumentChecking;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Base class for application commands that provides common functionality for command execution and validation.
/// </summary>
/// <typeparam name="TSettings">The type of settings used by the command.</typeparam>
/// <param name="validator">The validator used to validate command settings.</param>
/// <param name="exceptionHandler">The handler used to process exceptions that occur during command execution.</param>
public abstract class AppCommand<TSettings>(ICommandSettingsValidator<TSettings> validator, IExceptionHandler exceptionHandler) : Command<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    ///     Executes the command with the specified context and settings.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">A token that is forwarded to <see cref="DoExecute" /> so implementations can honour cancellation.</param>
    /// <returns>An integer representing the exit code of the command execution.</returns>
    /// <remarks>
    ///     Exceptions raised by <see cref="DoExecute" /> do not propagate: they are passed to the configured
    ///     <see cref="IExceptionHandler" />, whose result becomes the exit code.
    /// </remarks>
    public override int Execute(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        context.NotNull();
        settings.NotNull();

        try
        {
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
