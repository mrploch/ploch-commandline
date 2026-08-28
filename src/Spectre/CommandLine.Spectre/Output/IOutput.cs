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
    IOutput WriteLine();

    /// <summary>
    ///     Writes a message to the output followed by a line break.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to write.</param>
    /// <returns>The current output instance for method chaining.</returns>
    IOutput WriteLine<TMessage>(TMessage message);
}
