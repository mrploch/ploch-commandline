namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public interface IMessageHandler
{
    Type MessageType { get; }

    bool CanHandle(object? message);
}

public interface IMessageHandler<in TMessage> : IMessageHandler
{
    new Type MessageType => typeof(TMessage);

    new bool CanHandle(object? message) => message is TMessage;
}
