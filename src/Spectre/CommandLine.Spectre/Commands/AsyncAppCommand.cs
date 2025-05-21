using Ploch.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public abstract class AsyncAppCommand<TSettings>(ICommandSettingsValidator<TSettings> validator, IExceptionHandler<AsyncAppCommand<TSettings>> exceptionHandler)
    : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        context.NotNull();
        settings.NotNull();

        try
        {
            return (int)await DoExecuteAsync(context, settings);
        }
        catch (Exception ex)
        {
            return exceptionHandler.HandleException(ex);
        }
    }

    public override ValidationResult Validate(CommandContext context, TSettings settings) => validator.Validate(context, settings);

    protected abstract Task<ExitCode> DoExecuteAsync(CommandContext context, TSettings settings);
}
