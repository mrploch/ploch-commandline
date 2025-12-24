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
/// <param name="validator">The validator used to validate command settings.</param>
/// <param name="exceptionHandler">The handler used to process exceptions that occur during command execution.</param>
public abstract class AsyncAppCommand<TSettings>(CommandArgumentsRootProcessor settingsProcessor,
                                                 ICommandSettingsValidator<TSettings> validator,
                                                 IExceptionHandler exceptionHandler,
                                                 IOutput output) : AsyncCommand<TSettings> where TSettings : CommandSettings
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    protected IOutput Output => output;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    ///     Executes the command asynchronously with the provided context and settings.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <param name="cancellationToken">The cancellation token to use for the command execution.</param>
    /// <returns>An integer representing the exit code of the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or settings is null.</exception>
    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        context.NotNull();
        settings.NotNull();

        output.MarkupLineInterpolated($"Executing command [bold underline]{settings.GetType().Name}[/]");
        output.WriteLine();
        output.WriteLine("Processing arguments...");

        try
        {
            settingsProcessor.ProcessArguments(settings);

            return (int)await DoExecuteAsync(context, settings);
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
    /// <returns>A ValidationResult indicating whether the settings are valid.</returns>
    public override ValidationResult Validate(CommandContext context, TSettings settings) => validator.Validate(context, settings);

    /// <summary>
    ///     Implements the command's execution logic.
    /// </summary>
    /// <param name="context">The command context containing execution information.</param>
    /// <param name="settings">The settings to use for command execution.</param>
    /// <returns>A task representing the asynchronous operation that returns an ExitCode.</returns>
    protected abstract Task<ExitCode> DoExecuteAsync(CommandContext context, TSettings settings);
}
