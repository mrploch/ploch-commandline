using Ploch.Common.ArgumentChecking;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Base formatter for exception messages that handles formatting of exception type, message, and inner exceptions.
/// </summary>
/// <typeparam name="TException">The type of exception this formatter handles.</typeparam>
public class BaseExceptionMessageFormatter<TException> : TypeMessageFormatter<TException> where TException : Exception
{
    /// <summary>
    ///     Formats an exception into a human-readable message string.
    /// </summary>
    /// <param name="message">The exception to format. Cannot be null.</param>
    /// <param name="formatterProcessor">Optional formatter processor that can be used for additional formatting. Can be null.</param>
    /// <param name="formatProvider">
    ///     Unused. Exception text is already rendered, so there is nothing for a provider to format; the
    ///     parameter is present only to satisfy the contract.
    /// </param>
    /// <returns>A formatted string representation of the exception including type name, message, and inner exception details if present.</returns>
    public override string GetMessage(TException? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null)
    {
        message.NotNull();

        var text = GetExceptionText(message);

        text += GetInnerExceptionMessage(message?.InnerException);

        return text;
    }

    /// <summary>
    ///     Gets a formatted message for an inner exception if one exists.
    /// </summary>
    /// <param name="innerException">The inner exception to format.</param>
    /// <returns>A formatted string containing inner exception details if the inner exception is not null; otherwise, an empty string.</returns>
    protected static string GetInnerExceptionMessage(Exception? innerException)
    {
        if (innerException != null)
        {
            return $" / Inner exception: <{innerException.GetType().Name}> {innerException.Message}";
        }

        return string.Empty;
    }

    /// <summary>
    ///     Gets the formatted text representation of the exception.
    /// </summary>
    /// <param name="exception">The exception to format.</param>
    /// <returns>A string containing the exception type name and message.</returns>
    protected virtual string GetExceptionText(TException? exception) => $"<{exception?.GetType().Name}> {exception?.Message}";
}
