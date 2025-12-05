using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Provides an implementation of <see cref="IOutput" /> that uses Spectre.Console's ANSI console for markup-enabled
///     output.
/// </summary>
/// <param name="console">The ANSI console instance used for rendering output.</param>
/// <param name="formatterProcessor">The processor used for formatting messages.</param>
public class AnsiConsoleMarkupOutput(IAnsiConsole console, IMessageFormatterProcessor formatterProcessor) : IOutput
{
    /// <summary>
    ///     Writes a line terminator to the console.
    /// </summary>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput EndLine() => WriteLine();

    /// <summary>
    ///     Writes a markup-enabled interpolated string to the console.
    /// </summary>
    /// <param name="value">The interpolated string to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput MarkupInterpolated(FormattableString value)
    {
        console.MarkupInterpolated(value);

        return this;
    }

    /// <summary>
    ///     Writes a markup-enabled interpolated string followed by a line terminator to the console.
    /// </summary>
    /// <param name="value">The interpolated string to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput MarkupLineInterpolated(FormattableString value)
    {
        console.MarkupLineInterpolated(value);

        return this;
    }

    /// <summary>
    ///     Writes a message to the console with appropriate formatting based on the message type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <param name="format">The format provider to use for formatting, or null to use the current culture.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null)
    {
        if (message is FormattableString formattableString)
        {
            console.MarkupInterpolated(format ?? CultureInfo.CurrentCulture, formattableString);

            return this;
        }

        if (message is string str)
        {
            console.Markup(str);

            return this;
        }

        if (message is IRenderable renderable)
        {
            console.Write(renderable);

            return this;
        }

        formatterProcessor.WriteMessage(message);

        console.Write(message?.ToString() ?? string.Empty);

        return this;
    }

    /// <summary>
    ///     Writes a renderable object to the console.
    /// </summary>
    /// <param name="renderable">The renderable object to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput Write(IRenderable renderable)
    {
        console.Write(renderable);

        return this;
    }

    /// <summary>
    ///     Writes a message in bold formatting to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write in bold.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteBold<TMessage>(TMessage? message) => Write(formatterProcessor.GetMessageText(message, "bold"));

    /// <summary>
    ///     Writes a message in bold formatting followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write in bold.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteBoldLine<TMessage>(TMessage? message) => WriteLine(formatterProcessor.GetMessageText(message, "bold"));

    /// <summary>
    ///     Writes an error message in red formatting to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteError<TMessage>(TMessage? message) => Write(formatterProcessor.GetMessageText(message, "red"));

    /// <summary>
    ///     Writes an error message in red formatting followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteErrorLine<TMessage>(TMessage? message) => WriteLine(formatterProcessor.GetMessageText(message, "red"));

    /// <summary>
    ///     Writes an exception with detailed formatting to the console.
    /// </summary>
    /// <typeparam name="TException">The type of the exception to write.</typeparam>
    /// <param name="exception">The exception to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteException<TException>(TException? exception) where TException : Exception
    {
        if (exception is null)
        {
            return this;
        }

        console.WriteException(exception);

        return this;
    }

    /// <summary>
    ///     Writes a line terminator to the console.
    /// </summary>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteLine()
    {
        console.WriteLine();

        return this;
    }

    /// <summary>
    ///     Writes a message followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteLine<TMessage>(TMessage message)
    {
        if (message is FormattableString formattableString)
        {
            console.MarkupLineInterpolated(formattableString);

            return this;
        }

        if (message is string str)
        {
            console.MarkupLine(str);

            return this;
        }

        console.MarkupLine(message?.ToString() ?? string.Empty);

        return this;
    }

    /// <summary>
    ///     Writes a markup-enabled message followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The markup-enabled message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteMarkupLineInterpolated<TMessage>(TMessage message)
    {
        if (message is FormattableString str)
        {
            console.MarkupLineInterpolated(str);
        }

        return this;
    }
}
