using Ploch.Common.ArgumentChecking;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Provides an implementation of <see cref="IOutput" /> that uses Spectre.Console's ANSI console for markup-enabled
///     output.
/// </summary>
/// <remarks>
///     Markup is parsed only where the caller asks for it: <see cref="Markup" />, <see cref="MarkupLine" />,
///     <see cref="MarkupInterpolated" />, <see cref="MarkupLineInterpolated" /> and, by default through
///     <see cref="FormattableStringMessageWriter" />, a <see cref="FormattableString" /> passed to
///     <see cref="Write{TMessage}" /> or <see cref="WriteLine{TMessage}" />. A plain <see cref="string" /> is written
///     verbatim, which mirrors the split Spectre.Console itself makes between <c>IAnsiConsole.Write(string)</c> and
///     <c>IAnsiConsole.Markup(string)</c>.
///     <para>
///         <see cref="Write{TMessage}" /> and <see cref="WriteLine{TMessage}" /> render an <see cref="IRenderable" />
///         through the console and hand every other message to the registered <see cref="IMessageWriter" />
///         instances, including a <see cref="string" /> and a <see cref="FormattableString" />: the built-in
///         <see cref="StringMessageWriter" /> and <see cref="FormattableStringMessageWriter" /> are what write them,
///         so a consumer that registers a writer for either type changes how that type renders. Only a message no
///         writer accepts falls back to this class's own text rendering.
///     </para>
/// </remarks>
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
    ///     Writes a string that is parsed as Spectre.Console markup to the console.
    /// </summary>
    /// <param name="value">The markup to render.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="value" /> is not well-formed markup.</exception>
    public IOutput Markup(string value)
    {
        // Guarded here rather than left to Spectre so that Markup and MarkupLine share one contract: Spectre's
        // MarkupLine appends the terminator before parsing, which would turn a null into a silent blank line.
        value.NotNull();

        console.Markup(value);

        return this;
    }

    /// <summary>
    ///     Writes a string that is parsed as Spectre.Console markup to the console, followed by a line terminator.
    /// </summary>
    /// <param name="value">The markup to render.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="value" /> is not well-formed markup.</exception>
    public IOutput MarkupLine(string value)
    {
        value.NotNull();

        console.MarkupLine(value);

        return this;
    }

    /// <summary>
    ///     Writes a markup-enabled interpolated string to the console.
    /// </summary>
    /// <param name="value">The interpolated string to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public IOutput MarkupInterpolated(FormattableString value)
    {
        value.NotNull();

        console.MarkupInterpolated(value);

        return this;
    }

    /// <summary>
    ///     Writes a markup-enabled interpolated string followed by a line terminator to the console.
    /// </summary>
    /// <param name="value">The interpolated string to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public IOutput MarkupLineInterpolated(FormattableString value)
    {
        value.NotNull();

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
    /// <remarks>
    ///     An <see cref="IRenderable" /> is rendered by the console. Anything else, a <see cref="string" /> and a
    ///     <see cref="FormattableString" /> included, is offered to the registered <see cref="IMessageWriter" />
    ///     instances through <see cref="IMessageFormatterProcessor.WriteMessage{TMessage}" />, and only a message no
    ///     writer accepts is rendered as text here.
    /// </remarks>
    public IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null)
    {
        WriteCore(message, format);

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
    /// <remarks>
    ///     The processor returns the escaped text wrapped in the tag, so the result has to go through
    ///     <see cref="Markup" />: <see cref="Write{TMessage}" /> would print the tag literally.
    /// </remarks>
    public IOutput WriteBold<TMessage>(TMessage? message) => Markup(Decorate(message, "bold"));

    /// <summary>
    ///     Writes a message in bold formatting followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write in bold.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteBoldLine<TMessage>(TMessage? message) => MarkupLine(Decorate(message, "bold"));

    /// <summary>
    ///     Writes an error message in red formatting to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteError<TMessage>(TMessage? message) => Markup(Decorate(message, "red"));

    /// <summary>
    ///     Writes an error message in red formatting followed by a line terminator to the console.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    public IOutput WriteErrorLine<TMessage>(TMessage? message) => MarkupLine(Decorate(message, "red"));

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
    /// <remarks>
    ///     Dispatch is delegated to <see cref="Write{TMessage}" /> so that a message reaching this method sees the same
    ///     renderable handling and the same registered <see cref="IMessageWriter" /> instances it would through
    ///     <see cref="Write{TMessage}" />. Rendering the message here instead would make a custom registration take
    ///     effect on one method and not the other.
    ///     <para>
    ///         A terminator is still written unless the writer that rendered the message declares
    ///         <see cref="IMessageWriter.WritesLineTerminator" />, so a writer that renders inline keeps this method's
    ///         "followed by a line break" contract.
    ///     </para>
    /// </remarks>
    public IOutput WriteLine<TMessage>(TMessage message)
    {
        var writer = WriteCore(message);

        // Only a writer that declares it already emitted a terminator suppresses this one. Suppressing it for every
        // writer-handled message would silently drop the line break for a writer that renders inline, which is the
        // one thing WriteLine promises. EnumerableMessageWriter declares true because it writes a line per item.
        if (writer is { WritesLineTerminator: true })
        {
            return this;
        }

        return WriteLine();
    }

    /// <summary>
    ///     Renders a message that no registered writer accepted, honouring the caller's format provider.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to render.</typeparam>
    /// <param name="message">The message to render.</param>
    /// <param name="format">The format provider to apply, or <see langword="null" /> for the current culture.</param>
    /// <returns>The text to write, never <see langword="null" />.</returns>
    /// <remarks>
    ///     A parameterless <c>ToString()</c> would ignore the provider and format with the current culture, and the
    ///     result is coalesced because a custom <see cref="IFormattable" /> may return <see langword="null" /> -
    ///     which previously rendered as empty output rather than failing inside <c>console.Write</c>. A
    ///     <see cref="string" /> reaching this path - only possible when no writer accepts strings - is still written
    ///     verbatim; a <see cref="FormattableString" /> is formatted as plain text, without markup parsing, because
    ///     that parsing is <see cref="FormattableStringMessageWriter" />'s job.
    /// </remarks>
    private static string RenderFallbackText<TMessage>(TMessage message, IFormatProvider? format) => FormattedText.Render(message, format);

    /// <summary>
    ///     Asks the processor for the message text wrapped in <paramref name="markupTag" />, ready for
    ///     <see cref="Markup" />.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to decorate.</typeparam>
    /// <param name="message">The message to decorate.</param>
    /// <param name="markupTag">The style tag to wrap the message in.</param>
    /// <returns>The decorated markup, or an empty string when the processor returns none.</returns>
    /// <remarks>
    ///     <see cref="IMessageFormatterProcessor.GetMessageText{TMessage}" /> is allowed to return
    ///     <see langword="null" />, and <see cref="Markup" /> rejects a null argument, so the result is coalesced here
    ///     once rather than at each of the four decorating helpers.
    /// </remarks>
    private string Decorate<TMessage>(TMessage? message, string markupTag) => formatterProcessor.GetMessageText(message, markupTag) ?? string.Empty;

    /// <summary>
    ///     Renders a message and reports whether a registered <see cref="IMessageWriter" /> was the one that rendered it.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to write.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <param name="format">The format provider to use for formatting, or null to use the current culture.</param>
    /// <returns>
    ///     The registered <see cref="IMessageWriter" /> that rendered the message, or <see langword="null" /> when the
    ///     message was rendered directly by this class.
    /// </returns>
    /// <remarks>
    ///     A <see cref="string" /> and a <see cref="FormattableString" /> are not special-cased here. They used to be
    ///     written directly before the processor was consulted, which left the registered
    ///     <see cref="StringMessageWriter" /> and <see cref="FormattableStringMessageWriter" /> unreachable through
    ///     <see cref="IOutput" /> and made a consumer's own writer for either type a no-op. The only message type
    ///     rendered here is <see cref="IRenderable" />, which has no writer because the console renders it itself.
    /// </remarks>
    private IMessageWriter? WriteCore<TMessage>(TMessage message, IFormatProvider? format = null)
    {
        if (message is IRenderable renderable)
        {
            console.Write(renderable);

            return null;
        }

        var writer = formatterProcessor.WriteMessage(message, format);
        if (writer is not null)
        {
            return writer;
        }

        // A caller that supplied a provider expects it to be honoured. Parameterless ToString() ignores it and
        // formats with the current culture, so Write(1234.5, germanCulture) rendered "1234.5" instead of "1234,5".
        console.Write(RenderFallbackText(message, format));

        return null;
    }
}
