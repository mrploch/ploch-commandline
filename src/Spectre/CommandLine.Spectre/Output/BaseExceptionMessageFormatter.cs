using Ploch.Common;

namespace Ploch.CommandLine.Spectre.Output;

public class BaseExceptionMessageFormatter<TException> : TypeMessageFormatter<TException>
    where TException : Exception
{
    public override string GetMessage(TException? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        message.NotNull();

        var text = GetExceptionText(message);

        text += GetInnerExceptionMessage(message?.InnerException);

        return text;
    }

    protected virtual string GetExceptionText(TException? exception) => $"<{exception?.GetType().Name}> {exception?.Message}";

    protected string GetInnerExceptionMessage(Exception? innerException)
    {
        if (innerException != null)
        {
            return $" / Inner exception: <{innerException.GetType().Name}> {innerException.Message}";
        }

        return string.Empty;
    }
}
