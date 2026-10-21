using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Represents a console output interface for writing formatted text and objects.
/// </summary>
public interface IOutput
{
    /// <summary>
    ///     Ends the current line and starts a new line.
    /// </summary>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput EndLine();

    /// <summary>
    ///     Writes a string that is parsed as Spectre.Console markup to the output.
    /// </summary>
    /// <param name="value">The markup to render, such as <c>"[green]ok[/]"</c>.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <remarks>
    ///     This is the explicit opt-in to markup parsing: <see cref="Write{TMessage}" /> and
    ///     <see cref="WriteLine{TMessage}" /> write a <see cref="string" /> verbatim. Every <c>[</c> in
    ///     <paramref name="value" /> is read as the start of a style tag, so text that came from outside the source
    ///     file - a path, a user name, a configuration value - must be escaped with
    ///     <see cref="global::Spectre.Console.Markup.Escape(string)" /> first, or passed through <see cref="MarkupInterpolated" />,
    ///     which escapes its interpolation holes for you.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="value" /> is not well-formed markup, for example an unknown style name or an unbalanced tag.
    /// </exception>
    /// <example>
    ///     <code>
    /// output.Markup("[bold]Done:[/] ");
    /// output.Markup($"[green]{Markup.Escape(path)}[/]");   // or: output.MarkupInterpolated($"[green]{path}[/]")
    ///     </code>
    /// </example>
    IOutput Markup(string value);

    /// <summary>
    ///     Writes a string that is parsed as Spectre.Console markup to the output, followed by a line break.
    /// </summary>
    /// <param name="value">The markup to render, such as <c>"[green]ok[/]"</c>.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <remarks>
    ///     The line-terminating form of <see cref="Markup" />. The same escaping rule applies: data that did not
    ///     originate in the source file goes through <see cref="global::Spectre.Console.Markup.Escape(string)" /> or
    ///     <see cref="MarkupLineInterpolated" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="value" /> is not well-formed markup, for example an unknown style name or an unbalanced tag.
    /// </exception>
    IOutput MarkupLine(string value);

    /// <summary>
    ///     Writes markup-formatted interpolated string to the output.
    /// </summary>
    /// <param name="value">The interpolated string with markup formatting.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput MarkupInterpolated(FormattableString value);

    /// <summary>
    ///     Writes markup-formatted interpolated string to the output followed by a line break.
    /// </summary>
    /// <param name="value">The interpolated string with markup formatting.</param>
    /// <returns>The current output instance for method chaining.</returns>
    [SuppressMessage("ReSharper",
                     "UnusedMethodReturnValue.Global",
                     Justification = "Part of the public API: the return value exists for chaining by consumers, whether or not the library chains it.")]
    IOutput MarkupLineInterpolated(FormattableString value);

    /// <summary>
    ///     Writes a renderable object to the output.
    /// </summary>
    /// <param name="renderable">The renderable object to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput Write(IRenderable renderable);

    /// <summary>
    ///     Writes a message to the output with optional formatting.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <param name="format">The format provider to use for formatting the message, or null to use the default format.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <remarks>
    ///     The message is rendered by the registered <see cref="IMessageWriter" /> whose message type is the most
    ///     derived one it matches; an <see cref="IRenderable" /> is rendered by the console directly, and a message
    ///     no writer accepts is written as text. A <see cref="string" /> is therefore written by the registered
    ///     writer for strings, which by default writes it verbatim: a <c>[</c> in it is printed, not parsed as a
    ///     markup tag, so a path or user input can be passed straight through. To render markup, call
    ///     <see cref="Markup" /> or <see cref="MarkupInterpolated" />. A <see cref="FormattableString" /> message is
    ///     handed to the writer for that type, which by default renders it as interpolated markup with its holes
    ///     escaped, but note that an interpolated string literal infers <see cref="string" /> here, so it has to be
    ///     passed as <c>Write&lt;FormattableString&gt;($"...")</c> to take that path. Registering a writer for either
    ///     type with <c>AddMessageWriter</c> changes how it renders here and in <see cref="WriteLine{TMessage}" />;
    ///     the decorating members (<see cref="WriteBold{TMessage}" />, <see cref="WriteError{TMessage}" /> and their
    ///     line forms) go through the formatters and <see cref="Markup" /> instead, so a writer does not affect them.
    /// </remarks>
    IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null);

    /// <summary>
    ///     Writes a message to the output with bold formatting.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to write in bold.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteBold<TMessage>(TMessage? message);

    /// <summary>
    ///     Writes a message to the output with bold formatting followed by a line break.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to write in bold.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteBoldLine<TMessage>(TMessage? message);

    /// <summary>
    ///     Writes an error message to the output.
    /// </summary>
    /// <typeparam name="TMessage">The type of the error message.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteError<TMessage>(TMessage? message);

    /// <summary>
    ///     Writes an error message to the output followed by a line break.
    /// </summary>
    /// <typeparam name="TMessage">The type of the error message.</typeparam>
    /// <param name="message">The error message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteErrorLine<TMessage>(TMessage? message);

    /// <summary>
    ///     Writes an exception details to the output.
    /// </summary>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="exception">The exception to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteException<TException>(TException? exception) where TException : Exception;

    /// <summary>
    ///     Writes a line break to the output.
    /// </summary>
    /// <returns>The current output instance for method chaining.</returns>
    [SuppressMessage("ReSharper",
                     "UnusedMemberInSuper.Global",
                     Justification = "Part of the public API for consumers' commands; the library stopped calling it when the command preamble moved to standard error (PLO-783).")]
    IOutput WriteLine();

    /// <summary>
    ///     Writes a message to the output followed by a line break.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    /// <remarks>
    ///     The message takes the same path as in <see cref="Write{TMessage}" />, through the registered
    ///     <see cref="IMessageWriter" /> for its type, so a <see cref="string" /> is written verbatim by default and
    ///     a custom writer for a type applies here too. The line break is added afterwards unless the writer
    ///     reports <see cref="IMessageWriter.WritesLineTerminator" />. To render markup, call
    ///     <see cref="MarkupLine" /> or <see cref="MarkupLineInterpolated" />. An interpolated string literal infers
    ///     <see cref="string" /> here, so <c>WriteLine($"[green]ok[/]")</c> prints the tags; pass it as
    ///     <c>WriteLine&lt;FormattableString&gt;($"...")</c> to take the interpolated-markup path.
    /// </remarks>
    IOutput WriteLine<TMessage>(TMessage message);
}
