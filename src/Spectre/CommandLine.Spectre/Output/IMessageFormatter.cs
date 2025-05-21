namespace Ploch.CommandLine.Spectre.Output;

public interface IMessageFormatter : IMessageHandler
{
    string GetMessage(object message, IMessageFormatterProcessor formatterProcessor);
}

public interface IMessageFormatter<in TMessage> : IMessageFormatter
{
    new string GetMessage(object? message, IMessageFormatterProcessor? formatterProcessor = null) => GetMessage((TMessage?)message, formatterProcessor);

    string GetMessage(TMessage? message, IMessageFormatterProcessor? formatterProcessor = null);
}
