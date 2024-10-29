using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine;

public class AppConstructionContainer(CommandLineApplication application, IServiceCollection services, IConfiguration configuration)
{
    public CommandLineApplication Application { get; } = application;

    public IServiceCollection Services { get; } = services;

    public IConfiguration Configuration { get; } = configuration;

    public Func<IServiceProvider>? ServiceProviderFactory { get; set; }
}