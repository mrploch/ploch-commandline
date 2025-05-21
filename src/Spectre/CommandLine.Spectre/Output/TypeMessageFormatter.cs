namespace Ploch.CommandLine.Spectre.Output;

public abstract class TypeMessageFormatter<TMessage> : TypeMessageHandler<TMessage>, IMessageFormatter<TMessage>
{
    public string GetMessage(object message, IMessageFormatterProcessor? formatterProcessor = null) => GetMessage((TMessage?)message, formatterProcessor);

    public abstract string GetMessage(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);

    public virtual bool IsWriter => false;
}
