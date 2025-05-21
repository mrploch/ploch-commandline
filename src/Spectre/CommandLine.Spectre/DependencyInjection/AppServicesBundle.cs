using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.DependencyInjection;

public class AppServicesBundle : IServicesBundle
{
    private readonly Action<ILoggingBuilder>? _loggingBuilder;
    private IConfiguration? _configuration;

    public AppServicesBundle() : this(null)
    { }

    public AppServicesBundle(Action<ILoggingBuilder>? loggingBuilder = null, IConfiguration? configuration = null)
    {
        _loggingBuilder = loggingBuilder;
        _configuration = configuration;
    }

    public void Configure(IServiceCollection services)
    {
        _configuration ??= new ConfigurationBuilder().AddJsonFile("appsettings.json", true).Build();

        services.AddSingleton(AnsiConsole.Console)
                .AddSingleton(AnsiConsole.Console.Input)
                .AddSingleton(AnsiConsole.Console.Cursor)
                .AddSingleton(AnsiConsole.Console.ExclusivityMode)
                .AddSingleton(AnsiConsole.Console.Profile)
                .AddSingleton(typeof(ICommandSettingsValidator<>), typeof(CommandSettingsValidator<>))
                .AddSingleton(typeof(IExceptionHandler<>), typeof(ExceptionHandler<>));
        if (_loggingBuilder is null)
        {
            services.AddLogging(builder => builder.AddConsole());
        }
        else
        {
            services.AddLogging(_loggingBuilder);
        }
    }
}
