using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine
{
    public class AppConstructionContainer
    {
        public AppConstructionContainer(CommandLineApplication application, IServiceCollection serviceCollection, IConfiguration configuration)
        {
            Application = application;
            ServiceCollection = serviceCollection;
            Configuration = configuration;
        }

        public CommandLineApplication Application { get; }

        public IServiceCollection ServiceCollection { get; }

        public IConfiguration Configuration { get; }

        public Func<IServiceProvider>? ServiceProviderFactory { get; set; }
    }
}