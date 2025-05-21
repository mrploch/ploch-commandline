using Ploch.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public abstract class AppCommand<TSettings>(ICommandSettingsValidator<TSettings> validator, IExceptionHandler<AppCommand<TSettings>> exceptionHandler)
    : Command<TSettings>
    where TSettings : CommandSettings
{
    public override int Execute(CommandContext context, TSettings settings)
    {
        context.NotNull();
        settings.NotNull();

        try
        {
            return (int)DoExecute(context, settings);
        }
        catch (Exception ex)
        {
            return exceptionHandler.HandleException(ex);
        }
    }

    public override ValidationResult Validate(CommandContext context, TSettings settings) => validator.Validate(context, settings);

    protected abstract ExitCode DoExecute(CommandContext context, TSettings settings);
}
