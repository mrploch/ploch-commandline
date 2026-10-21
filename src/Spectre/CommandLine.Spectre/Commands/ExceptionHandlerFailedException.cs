namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Thrown by <see cref="AppCommand{TSettings}" /> and <see cref="AsyncAppCommand{TSettings}" /> when the
///     registered <see cref="IExceptionHandler" /> throws while handling an exception.
/// </summary>
/// <remarks>
///     The handler <see cref="AppBuilder.ConfigureCommandApp" /> installs reports the handler's exception once and
///     exits with <see cref="ExitCode.Error" />, without asking the same handler to handle its own exception. An
///     application that switched Spectre's handling off with <c>PropagateExceptions()</c> sees this exception
///     instead; <see cref="Exception.InnerException" /> is what the handler threw and <see cref="Original" /> is what
///     it was asked to handle.
/// </remarks>
public sealed class ExceptionHandlerFailedException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionHandlerFailedException" /> class.
    /// </summary>
    public ExceptionHandlerFailedException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionHandlerFailedException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ExceptionHandlerFailedException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionHandlerFailedException" /> class with a message and
    ///     the exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception the handler threw.</param>
    public ExceptionHandlerFailedException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionHandlerFailedException" /> class for a handler that
    ///     threw <paramref name="handlerException" /> while handling <paramref name="original" />.
    /// </summary>
    /// <param name="original">The exception the handler was asked to handle.</param>
    /// <param name="handlerException">The exception the handler threw.</param>
    public ExceptionHandlerFailedException(Exception original, Exception handlerException)
        : base($"The exception handler threw {handlerException?.GetType().Name} while handling {original?.GetType().Name}: {handlerException?.Message}",
               handlerException)
    {
        Original = original;
    }

    /// <summary>
    ///     Gets the exception the handler was asked to handle, or <see langword="null" /> when the instance was not
    ///     created for a failed handler call.
    /// </summary>
    public Exception? Original { get; }
}
