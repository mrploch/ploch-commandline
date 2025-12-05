using Microsoft.Extensions.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to register message formatters and writers.
/// </summary>
public static class ServiceCollectionOutputExtensions
{
    /// <summary>
    ///     Registers a message formatter for a specific message type in the service collection.
    /// </summary>
    /// <typeparam name="TMessageType">The type of message that the formatter can format.</typeparam>
    /// <typeparam name="TFormatter">The type of the formatter implementation.</typeparam>
    /// <param name="service">The service collection to add the formatter to.</param>
    /// <returns>The same service collection instance to enable method chaining.</returns>
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
    public static IServiceCollection AddMessageWriter<TMessageType, TWriter>(this IServiceCollection service)
        where TWriter : class, IMessageWriter<TMessageType> =>
        service.AddSingleton<IMessageWriter, TWriter>().AddSingleton<IMessageWriter<TMessageType>, TWriter>();
}
