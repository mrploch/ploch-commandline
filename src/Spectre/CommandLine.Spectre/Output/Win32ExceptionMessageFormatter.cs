using System.ComponentModel;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Formats <see cref="Win32Exception" /> instances, including the native error code in the rendered message.
/// </summary>
public class Win32ExceptionMessageFormatter : BaseExceptionMessageFormatter<Win32Exception>
{
    /// <summary>
    ///     Builds an interpolated, unmarked-up description of the exception including its native error code.
    /// </summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>A formattable string describing the exception.</returns>
    protected static FormattableString GetFormattedExceptionText(Win32Exception exception) =>
        $"[{exception.GetType().Name}] [Error Code: {exception.NativeErrorCode}] {exception.Message}";

    /// <summary>
    ///     Builds the marked-up console text for the exception, including its native error code.
    /// </summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>The Spectre.Console markup describing the exception.</returns>
    protected override string GetExceptionText(Win32Exception? exception) =>
        $"<{exception?.GetType().Name}> [underline]<Error Code: {exception?.NativeErrorCode}>[/] {exception?.Message}";
}
