using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine;

public class AppBuilder
{
    private readonly Func<CommandLineApplication> _appBuildFunc;
    private readonly List<Action<AppConstructionContainer>> _configurationActions = new();
    private readonly Action<IConfigurationBuilder> _configurationBuilderAction;
    private readonly List<Action<IConfigurationBuilder>> _configurationBuilderActions = new();

    private AppBuilder(Func<CommandLineApplication>? appBuildFunc,
        Action<IConfigurationBuilder> configurationBuilderAction,
        params Action<AppConstructionContainer>[] configurationActions)
    {
        _configurationBuilderAction = configurationBuilderAction;
        // _configurationBuilderActions.Add(builder => ConfigurationSetup.DefaultFileConfiguration(builder));

        SetupDefaults();

        _appBuildFunc = appBuildFunc ?? (() => new CommandLineApplication());
        foreach (var configurationAction in configurationActions)
        {
            _configurationActions.Add(configurationAction);
        }
    }

    public static AppBuilder CreateDefault(Action<IConfigurationBuilder>? configurationAction = null, params string[] args)
    {
        var builder = new AppBuilder(() => new CommandLineApplication(),
            configurationBuilder => configurationBuilder.AddCommandLine(args),
            container => container.Application.Conventions.UseDefaultConventions().UseCommandAttribute().UseCommandNameFromModelType());

        return builder.WithConfiguration(configurationAction ?? (configurationBuilder => ConfigurationSetup.DefaultFileConfiguration(configurationBuilder)));
    }

    public AppBuilder Configure(Action<AppConstructionContainer> configurationAction)
    {
        _configurationActions.Add(configurationAction);

        return this;
    }

    public CommandLineApplication Build()
    {
        var app = _appBuildFunc();
        var serviceCollections = new ServiceCollection();

        var configurationBuilder = new ConfigurationBuilder();

        _configurationBuilderActions.Add(_configurationBuilderAction);

        foreach (var configurationBuilderAction in _configurationBuilderActions)
        {
            configurationBuilderAction(configurationBuilder);
        }

        var configuration = configurationBuilder.Build();

        var configurationContainer = new AppConstructionContainer(app, serviceCollections, configuration);

        foreach (var configurationAction in _configurationActions)
        {
            configurationAction(configurationContainer);
        }

        var serviceProviderFactory = configurationContainer.ServiceProviderFactory ?? (() => configurationContainer.ServiceCollection.BuildServiceProvider());

        app.Conventions.UseConstructorInjection(serviceProviderFactory());

        return app;
    }

    private void SetupDefaults()
    {
        _configurationBuilderActions.Add(builder => ConfigurationSetup.DefaultFileConfiguration(builder));
    }

    private AppBuilder WithDefaultConfiguration(IEnumerable<string>? configurationFileNames = null, Action<IConfigurationBuilder>? configurationBuilderAction = null)
    {
        _configurationBuilderActions.Insert(0, builder => ConfigurationSetup.DefaultFileConfiguration(builder, configurationFileNames, configurationBuilderAction));

        return this;
    }

    private AppBuilder WithConfiguration(Action<IConfigurationBuilder> configurationBuilderAction)
    {
        _configurationBuilderActions.Add(configurationBuilderAction);

        return this;
    }
}