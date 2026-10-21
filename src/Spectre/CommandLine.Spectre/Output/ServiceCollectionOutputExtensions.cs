using Microsoft.Extensions.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to register message formatters and writers.
/// </summary>
/// <remarks>
///     <see cref="MessageFormatterProcessor" /> selects the handler whose message type is the most derived among
///     those that accept a message, and of two handlers for the same type it uses the later registration. The
///     built-in handlers are registered by the output services bundle before any <c>ConfigureServices</c> delegate
///     runs, so registering a handler for a type the bundle already covers - such as <see cref="string" />,
///     <see cref="FormattableString" />, <see cref="Exception" /> or <see cref="System.Collections.IEnumerable" /> -
///     replaces the built-in one for that type.
/// </remarks>
public static class ServiceCollectionOutputExtensions
{
    /// <summary>
    ///     Registers a message formatter for a specific message type in the service collection.
    /// </summary>
    /// <typeparam name="TMessageType">The type of message that the formatter can format.</typeparam>
    /// <typeparam name="TFormatter">The type of the formatter implementation.</typeparam>
    /// <param name="service">The service collection to add the formatter to.</param>
    /// <returns>The same service collection instance to enable method chaining.</returns>
    /// <remarks>
    ///     A formatter registered for a type that already has one replaces it for that exact type; see the class
    ///     remarks.
    /// </remarks>
    public static IServiceCollection AddMessageFormatter<TMessageType, TFormatter>(this IServiceCollection service)
        where TFormatter : class, IMessageFormatter<TMessageType> =>
        service.AddSingleton<IMessageFormatter, TFormatter>().AddSingleton<IMessageFormatter<TMessageType>, TFormatter>();

    /// <summary>
    ///     Registers a message writer for a specific message type in the service collection.
    /// </summary>
    /// <typeparam name="TMessageType">The type of message that the writer can write.</typeparam>
    /// <typeparam name="TWriter">The type of the writer implementation.</typeparam>
    /// <param name="service">The service collection to add the writer to.</param>
    /// <returns>The same service collection instance to enable method chaining.</returns>
    /// <remarks>
    ///     A writer registered for a type that already has one replaces it for that exact type, so this is how the
    ///     rendering of a <see cref="string" /> or a <see cref="FormattableString" /> written through
    ///     <see cref="IOutput.Write{TMessage}" /> is changed. A writer takes the <c>IAnsiConsole</c> it renders to,
    ///     not <see cref="IOutput" />, which depends on the writers.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public sealed class QuotingStringWriter(IAnsiConsole console) : TypeMessageWriter&lt;string&gt;
    /// {
    ///     public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =&gt;
    ///         console.Write($"\"{message}\"");
    /// }
    ///
    /// AppBuilder.Create(args)
    ///           .ConfigureServices(services =&gt; services.AddMessageWriter&lt;string, QuotingStringWriter&gt;());
    ///     </code>
    /// </example>
    public static IServiceCollection AddMessageWriter<TMessageType, TWriter>(this IServiceCollection service)
        where TWriter : class, IMessageWriter<TMessageType> =>
        service.AddSingleton<IMessageWriter, TWriter>().AddSingleton<IMessageWriter<TMessageType>, TWriter>();
}
