using System.Collections;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Output;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Configuration;

/// <summary>
///     Provides a bundle of output-related services for dependency injection.
///     This class configures console output, message formatting, and message writing services.
/// </summary>
public class OutputServicesBundle : ServicesBundle
{
    /// <summary>
    ///     Configures the output services in the dependency injection container.
    ///     Registers console, output, message formatter processor, message writers, and message formatters.
    /// </summary>
    public override void DoConfigure()
    {
        Services.AddSingleton(AnsiConsole.Console)
                .AddSingleton<IOutput, AnsiConsoleMarkupOutput>()
                .AddSingleton<IMessageFormatterProcessor, MessageFormatterProcessor>();

        AddMessageWriters();
        AddMessageFormatters();
    }

    /// <summary>
    ///     Registers message writers for different types of messages in the dependency injection container.
    /// </summary>
    private void AddMessageWriters() => Services.AddMessageWriter<FormattableString, FormattableStringMessageWriter>()
                                                .AddMessageWriter<Exception, ExceptionMessageWriter>()
                                                .AddMessageWriter<string, StringMessageWriter>()
                                                .AddMessageWriter<IEnumerable, EnumerableMessageWriter>();

    /// <summary>
    ///     Registers message formatters for different types of messages in the dependency injection container.
    /// </summary>
    private void AddMessageFormatters() => Services.AddMessageFormatter<Exception, ExceptionMessageFormatter>()
                                                   .AddMessageFormatter<Win32Exception, Win32ExceptionMessageFormatter>()
                                                   .AddMessageFormatter<string, StringMessageFormatter>()
                                                   .AddMessageFormatter<IEnumerable, EnumerableMessageFormatter>()
                                                   .AddMessageFormatter<IConvertible, ConvertibleMessageFormatter>();
}
