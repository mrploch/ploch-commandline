using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Ploch.CommandLine.Hosting.Internal;

namespace Ploch.CommandLine.Hosting;

public static class ServiceCollectionRegistrations
{
    /*public static IHostBuilder UseCommandLineApplication<TApp>(
           this IHostBuilder hostBuilder,
           string[] args,
           Action<CommandLineApplication<TApp>> configure)
           where TApp : class
       {
           configure ??= _ => { };
           var state = new CommandLineState(args);
           hostBuilder.Properties[typeof(CommandLineState)] = state;
           hostBuilder.ConfigureServices((context, services) =>
               services
                   .AddCommonServices(state)
                   .AddSingleton<ICommandLineService, CommandLineService<TApp>>()
                   .AddSingleton(configure));

           return hostBuilder;
       }*
    */
    public static IServiceCollection AddCommandLineAppServices<TApp>(IServiceCollection services,
                                                                     IEnumerable<string> args,
                                                                     Action<CommandLineApplication<TApp>> configure)
        where TApp : class
    {
        // ReSharper disable PossibleMultipleEnumeration
        var state = new CommandLineState(args);

        return services.AddCommonServices(state)
            .AddSingleton<ICommandLineService, CommandLineService<TApp>>()
            .AddSingleton(configure);

        // ReSharper restore PossibleMultipleEnumeration
    }

    private static IServiceCollection AddCommonServices(this IServiceCollection services, CommandLineState state)
    {
        services.TryAddSingleton<StoreExceptionHandler>();
        services.TryAddSingleton<IUnhandledExceptionHandler>(provider => provider.GetRequiredService<StoreExceptionHandler>());
        services.TryAddSingleton(PhysicalConsole.Singleton);
        services
            .AddSingleton<IHostLifetime, CommandLineLifetime>()
            .AddSingleton(provider =>
                          {
                              state.SetConsole(provider.GetRequiredService<IConsole>());
                              return state;
                          })
            .AddSingleton<CommandLineContext>(state);

        return services;
    }
}