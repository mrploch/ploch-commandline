namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public interface IMessageWriter : IMessageHandler
{
    void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null);
}

public interface IMessageWriter<in TMessage> : IMessageWriter
{
    new Type MessageType => typeof(TMessage);

    new bool CanHandle(object? message) => message is TMessage;

    void Write(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);
}
