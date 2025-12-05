namespace Ploch.CommandLine.Spectre.Output;

public abstract class TypeMessageHandler<TMessage> : IMessageHandler
{
    public virtual Type MessageType => typeof(TMessage);

    public virtual bool CanHandle(object? message) => MessageType.IsInstanceOfType(message);
}
