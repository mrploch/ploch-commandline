namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public abstract class TypeMessageHandler<TMessage> : IMessageHandler<TMessage>
{
    public virtual Type MessageType => typeof(TMessage);

    public virtual bool CanHandle(object? message) => message is TMessage;
}
