namespace Ploch.CommandLine.Spectre.Output;

public abstract class TypeMessageHandler<TMessage> : IMessageHandler<TMessage>
{
    public virtual Type MessageType => typeof(TMessage);

    public virtual bool CanHandle(object? message) => message is TMessage;
}
