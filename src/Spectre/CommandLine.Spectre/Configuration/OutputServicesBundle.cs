using System.Collections;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Output;

/// <summary>
///     Provides a bundle of output-related services for dependency injection.
///     This class configures console output, message formatting, and message writing services.
/// </summary>
public class OutputServicesBundle : IServicesBundle
{
#pragma warning disable CA2263
    /// <summary>
    ///     Configures the output services in the dependency injection container.
    ///     Registers console, output, message formatter processor, message writers, and message formatters.
    /// </summary>
    /// <param name="services">The service collection to configure with output-related services.</param>
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(AnsiConsole.Console)
                .AddSingleton<IOutput, AnsiConsoleMarkupOutput>()
                .AddSingleton<IMessageFormatterProcessor, MessageFormatterProcessor>();

        AddMessageWriters(services);
        AddMessageFormatters(services);
    }

    /// <summary>
    ///     Registers message writers for different types of messages in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure with message writers.</param>
    private static void AddMessageWriters(IServiceCollection services) => services.AddMessageWriter<FormattableString, FormattableStringMessageWriter>()
                                                                                  .AddMessageWriter<Exception, ExceptionMessageWriter>()
                                                                                  .AddMessageWriter<string, StringMessageWriter>()
                                                                                  .AddMessageWriter<IEnumerable, EnumerableMessageWriter>();

    /// <summary>
    ///     Registers message formatters for different types of messages in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure with message formatters.</param>
    private static void AddMessageFormatters(IServiceCollection services) => services.AddMessageFormatter<Exception, ExceptionMessageFormatter>()
                                                                                     .AddMessageFormatter<Win32Exception, Win32ExceptionMessageFormatter>()
                                                                                     .AddMessageFormatter<string, StringMessageFormatter>()
                                                                                     .AddMessageFormatter<IEnumerable, EnumerableMessageFormatter>()
                                                                                     .AddMessageFormatter<IConvertible, ConvertibleMessageFormatter>();
}
