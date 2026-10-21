namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Calls the registered <see cref="IExceptionHandler" /> on behalf of the command base classes and the Spectre
///     handler, so a handler that throws surfaces as one <see cref="ExceptionHandlerFailedException" /> everywhere.
/// </summary>
internal static class ExceptionHandlerInvoker
{
    /// <summary>
    ///     Calls <paramref name="handler" /> for <paramref name="exception" />, wrapping anything it throws in an
    ///     <see cref="ExceptionHandlerFailedException" />.
    /// </summary>
    /// <param name="handler">The registered exception handler.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>The exit code the handler returned.</returns>
    /// <exception cref="ExceptionHandlerFailedException">The handler threw.</exception>
    public static int Invoke(IExceptionHandler handler, Exception exception)
    {
        try
        {
            return handler.HandleException(exception);
        }
        catch (Exception handlerException)
        {
            throw new ExceptionHandlerFailedException(exception, handlerException);
        }
    }
}
