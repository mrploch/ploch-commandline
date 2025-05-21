using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Output;

public interface IOutput
{
    IOutput EndLine();

    IOutput MarkupInterpolated(FormattableString value);

    IOutput MarkupLineInterpolated(FormattableString value);

    IOutput Write(IRenderable renderable);

    IOutput Write<TMessage>(TMessage message);

    IOutput WriteBold<TMessage>(TMessage? message);

    IOutput WriteBoldLine<TMessage>(TMessage? message);

    IOutput WriteError<TMessage>(TMessage? message);

    IOutput WriteErrorLine<TMessage>(TMessage? message);

    IOutput WriteException<TException>(TException? exception)
        where TException : Exception;

    IOutput WriteLine();

    IOutput WriteLine<TMessage>(TMessage message);
}
