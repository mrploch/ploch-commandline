namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public abstract class TypeMessageWriter<TMessage> : TypeMessageHandler<TMessage>, IMessageWriter<TMessage>
{
    public abstract void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);

    public void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null) => Write((TMessage?)message, formatterProcessor);
}
