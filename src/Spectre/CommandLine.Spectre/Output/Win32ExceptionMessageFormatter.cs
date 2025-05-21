using System.ComponentModel;

namespace Ploch.CommandLine.Spectre.Output;

public class Win32ExceptionMessageFormatter : BaseExceptionMessageFormatter<Win32Exception>
{
    protected override string GetExceptionText(Win32Exception exception) =>
        $"<{exception.GetType().Name}> [underline]<Error Code: {exception.NativeErrorCode}>[/] {exception.Message}";

    protected FormattableString GetFormattedExceptionText(Win32Exception exception) =>
        $"[{exception.GetType().Name}] [Error Code: {exception.NativeErrorCode}] {exception.Message}";
}
