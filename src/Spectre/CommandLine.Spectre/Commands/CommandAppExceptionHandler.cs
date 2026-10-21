using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     The exception handler <see cref="AppBuilder.ConfigureCommandApp" /> installs on the Spectre.Console.Cli
///     application, so that the exit codes a shell sees are the ones <see cref="ExitCode" /> documents.
/// </summary>
/// <remarks>
///     <para>
///         Spectre.Console.Cli hands this handler every exception it catches itself: a command line it could not parse
///         (<see cref="CommandParseException" />), settings it could not bind or that failed validation
///         (<see cref="CommandRuntimeException" />), and whatever escapes a command that does not derive from
///         <see cref="AppCommand{TSettings}" /> or <see cref="AsyncAppCommand{TSettings}" />. Without a handler Spectre
///         renders its own message and returns <c>-1</c> for all of them, which a POSIX shell reports as <c>255</c>.
///     </para>
///     <para>
///         The handler keeps Spectre's rendering - the pretty parse error with its caret, or <c>Error: message</c> -
///         and maps the exception to an exit code: a parse or binding failure is
///         <see cref="ExitCode.InvalidInput" />; an <see cref="OperationCanceledException" /> after the application's
///         own token was cancelled is the configured cancellation exit code
///         (<see cref="ICommandAppSettings.CancellationExitCode" />, <see cref="ExitCode.Cancelled" /> by default);
///         anything else goes to the registered <see cref="IExceptionHandler" />, whose return value is the exit code.
///         When no host exists yet - the failure happened while it was being built - or the handler itself throws
///         or cannot be constructed, the exception is rendered the way Spectre would have rendered it and the exit
///         code is <see cref="ExitCode.Error" />, so a failure is always reported and never exits <c>0</c>.
///     </para>
///     <para>
///         Spectre raises <see cref="CommandRuntimeException" /> for a few failures that are not input errors - a missing
///         type converter, a command whose dependencies could not be resolved. They share the type with the validation
///         and binding failures and carry nothing that tells them apart, so they exit with
///         <see cref="ExitCode.InvalidInput" /> too. An application that needs a different mapping installs its own
///         handler through <c>SetExceptionHandler</c> in the delegate it passes to
///         <see cref="AppBuilder.ConfigureCommandApp" />, which replaces this one.
///     </para>
/// </remarks>
/// <param name="cancellationToken">The token the application cancels through; see <see cref="AppBuilder" />.</param>
internal sealed class CommandAppExceptionHandler(CancellationToken cancellationToken)
{
    /// <summary>
    ///     Handles an exception Spectre.Console.Cli caught and returns the exit code of the run.
    /// </summary>
    /// <param name="exception">The exception Spectre caught.</param>
    /// <param name="resolver">
    ///     Spectre's type resolver over the application's host, or <see langword="null" /> when the exception happened
    ///     before a host existed - while parsing the command line or building the host.
    /// </param>
    /// <param name="settings">The application settings, for the console to render on and the cancellation exit code.</param>
    /// <returns>The exit code of the run.</returns>
    public int Handle(Exception exception, ITypeResolver? resolver, ICommandAppSettings settings)
    {
        var console = settings.Console ?? AnsiConsole.Console;

        // A handler that threw, from a command base class or from the call below. Reported once and the run exits
        // non-zero; the handler is not asked to handle the exception it threw itself, and the exception it threw is
        // never mistaken for an input error or a cancellation.
        if (exception is ExceptionHandlerFailedException failed)
        {
            // The public constructors allow an instance without an inner exception; such an instance is reported itself.
            Render(console, failed.InnerException ?? failed);

            return (int)ExitCode.Error;
        }

        // Cancellation first: an OperationCanceledException raised after the application's own token was cancelled is
        // the outcome the user asked for, whatever stage it surfaced at. One raised while the token is still live is a
        // fault nobody asked for and takes the handler path below (PLO-756).
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return settings.CancellationExitCode;
        }

        // CommandParseException derives from CommandRuntimeException, so this is every parse, binding and validation
        // failure Spectre raises.
        if (exception is CommandRuntimeException)
        {
            Render(console, exception);

            return (int)ExitCode.InvalidInput;
        }

        IExceptionHandler? handler;
        try
        {
            handler = ResolveExceptionHandler(resolver);
        }
        catch (Exception resolutionException)
        {
            // The handler is registered but could not be constructed. Both failures are reported: the one the command
            // raised and the configuration defect that stopped it being handled.
            Render(console, exception);
            Render(console, resolutionException);

            return (int)ExitCode.Error;
        }

        if (handler is null)
        {
            Render(console, exception);

            return (int)ExitCode.Error;
        }

        try
        {
            return ExceptionHandlerInvoker.Invoke(handler, exception);
        }
        catch (ExceptionHandlerFailedException handlerFailed)
        {
            Render(console, handlerFailed.InnerException ?? handlerFailed);

            return (int)ExitCode.Error;
        }
    }

    /// <summary>
    ///     Resolves the registered <see cref="IExceptionHandler" />, or <see langword="null" /> when there is no host
    ///     yet or the host has none.
    /// </summary>
    /// <param name="resolver">Spectre's type resolver over the host, or <see langword="null" /> before a host exists.</param>
    /// <returns>The registered handler, or <see langword="null" />.</returns>
    /// <exception cref="Exception">The handler is registered but its construction failed.</exception>
    private static IExceptionHandler? ResolveExceptionHandler(ITypeResolver? resolver)
    {
        if (resolver is null)
        {
            return null;
        }

        // Through the container's IServiceProvider rather than by asking the resolver for the handler type: Spectre's
        // resolver adapter falls back to Activator.CreateInstance for a type the container does not know, which fails
        // for an interface and would hide "no handler registered" behind a CommandRuntimeException. GetService returns
        // null for an absent service and throws only when a registered one cannot be constructed.
        var services = resolver.Resolve(typeof(IServiceProvider)) as IServiceProvider;

        return services?.GetService(typeof(IExceptionHandler)) as IExceptionHandler;
    }

    /// <summary>
    ///     Renders an exception the way Spectre.Console.Cli renders the ones it handles itself: the exception's own
    ///     pretty renderable when it has one, otherwise <c>Error: message</c>, followed by the pretty renderable of its
    ///     inner exception when that one has one.
    /// </summary>
    /// <param name="console">The console to render on.</param>
    /// <param name="exception">The exception to render.</param>
    private static void Render(IAnsiConsole console, Exception exception)
    {
        if (exception is CommandAppException { Pretty: not null } pretty)
        {
            console.Write(pretty.Pretty);

            return;
        }

        console.MarkupLine($"[red]Error:[/] {exception.Message.EscapeMarkup()}");

        if (exception.InnerException is CommandAppException { Pretty: not null } inner)
        {
            console.Write(inner.Pretty);
        }
    }
}
