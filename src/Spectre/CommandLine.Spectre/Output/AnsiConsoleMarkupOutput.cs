using Ploch.Tools.SystemProfiles.Core;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public class AnsiConsoleMarkupOutput(IMessageFormatterProcessor formatterProcessor) : IOutput
{
    public IOutput EndLine()
    {
        AnsiConsole.Write('\n');

        return this;
    }

    public IOutput MarkupInterpolated(FormattableString value)
    {
        AnsiConsole.MarkupInterpolated(value);

        return this;
    }

    public IOutput MarkupLineInterpolated(FormattableString value)
    {
        AnsiConsole.MarkupLineInterpolated(value);

        return this;
    }

    public IOutput Write<TMessage>(TMessage message)
    {
        if (message is FormattableString formattableString)
        {
            AnsiConsole.MarkupInterpolated(formattableString);

            return this;
        }

        if (message is string str)
        {
            AnsiConsole.Markup(str);

            return this;
        }

        if (message is IRenderable renderable)
        {
            AnsiConsole.Write(renderable);

            return this;
        }

        formatterProcessor.WriteMessage(message);

        AnsiConsole.Write(message.ToString());

        return this;
    }

    public IOutput Write(IRenderable renderable)
    {
        AnsiConsole.Write(renderable);

        return this;
    }

    public IOutput WriteBold<TMessage>(TMessage? message) => Write(formatterProcessor.GetMessageText(message, "bold"));

    public IOutput WriteBoldLine<TMessage>(TMessage? message) => WriteLine(formatterProcessor.GetMessageText(message, "bold"));

    public IOutput WriteError<TMessage>(TMessage? message) => Write(formatterProcessor.GetMessageText(message, "red"));

    public IOutput WriteErrorLine<TMessage>(TMessage? message) => WriteLine(formatterProcessor.GetMessageText(message, "red"));

    public IOutput WriteException<TException>(TException exception)
        where TException : Exception
    {
        AnsiConsole.WriteException(exception);

        return this;
    }

    public IOutput WriteLine()
    {
        AnsiConsole.WriteLine();

        return this;
    }

    public IOutput WriteLine<TMessage>(TMessage message)
    {
        if (message is FormattableString formattableString)
        {
            AnsiConsole.MarkupLineInterpolated(formattableString);

            return this;
        }

        if (message is string str)
        {
            AnsiConsole.MarkupLine(str);

            return this;
        }

        AnsiConsole.MarkupLine(message.ToString());

        return this;
    }

    public IOutput WriteMarkupLineInterpolated<TMessage>(TMessage message)
    {
        if (message is FormattableString str)
        {
            AnsiConsole.MarkupLineInterpolated(str);
        }

        return this;
    }
}
