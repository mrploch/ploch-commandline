using Ploch.Common.ArgumentChecking;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Announces the command about to run — <c>Executing command &lt;name&gt;</c> — on the console given to it, which
///     <see cref="AppBuilder" /> makes the standard error console, so the line never mixes with the command's own output.
/// </summary>
/// <param name="errorConsole">The console the announcement is written to.</param>
/// <remarks>
///     <para>
///         Spectre.Console.Cli resolves every registered <see cref="ICommandInterceptor" /> and calls
///         <see cref="Intercept" /> once the settings are bound, before the command's <c>Validate</c> and
///         <c>Execute</c>. The line therefore names the command by the name it is registered under
///         (<see cref="CommandContext.Name" />, the leaf command name; an alias is announced by the registered name)
///         rather than the settings type, and it appears for every command the application runs,
///         not only those deriving from <see cref="AppCommand{TSettings}" /> or <see cref="AsyncAppCommand{TSettings}" />.
///         A default command, which Spectre runs when no command name was typed, is announced as "the default command"
///         rather than by the placeholder name Spectre gives it.
///     </para>
///     <para>
///         An exception thrown while writing — a closed standard error stream, say — propagates to Spectre, which
///         handles it as it would any failure before a command runs; it does not reach the <see cref="IExceptionHandler" />
///         the command would have used.
///     </para>
/// </remarks>
internal sealed class ExecutingCommandInterceptor(IAnsiConsole errorConsole) : ICommandInterceptor
{
    /// <summary>
    ///     The name Spectre.Console.Cli 0.53 gives the default command in the parsed tree, and so in
    ///     <see cref="CommandContext.Name" />, when the arguments named no command. An internal constant of Spectre's
    ///     (<c>CliConstants.DefaultCommandName</c>), pinned here by a test that runs a real default command.
    /// </summary>
    internal const string SpectreDefaultCommandName = "__default_command";

    private readonly IAnsiConsole _errorConsole = errorConsole.NotNull();

    /// <inheritdoc />
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        context.NotNull();

        if (context.Name == SpectreDefaultCommandName)
        {
            _errorConsole.MarkupLine("Executing [bold underline]the default command[/]");

            return;
        }

        // Interpolated, not concatenated: the hole is escaped, so a command name containing brackets is rendered as
        // text rather than parsed as a style tag.
        _errorConsole.MarkupLineInterpolated($"Executing command [bold underline]{context.Name}[/]");
    }

    /// <inheritdoc />
    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        // The exit code is the command's to decide.
    }
}
