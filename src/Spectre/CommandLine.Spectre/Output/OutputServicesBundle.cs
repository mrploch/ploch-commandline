using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.DependencyInjection;

namespace Ploch.CommandLine.Spectre.Output;

public class OutputServicesBundle : IServicesBundle
{
    public void Configure(IServiceCollection services) => services.AddSingleton<IOutput, AnsiConsoleMarkupOutput>()
                                                                  .AddSingleton<IMessageFormatterProcessor, MessageFormatterProcessor>()
                                                                  .AddSingleton<IMessageFormatter, ExceptionMessageFormatter>()
                                                                  .AddSingleton<IMessageFormatter, Win32ExceptionMessageFormatter>()
                                                                  .AddSingleton<IMessageFormatter, StringMessageFormatter>()
                                                                  .AddSingleton<IMessageFormatter, EnumerableMessageFormatter<IEnumerable<string>>>();
}
