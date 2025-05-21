using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine;

/// <summary>
///     A builder class for constructing instances of <see cref="CommandLineApplication" />
///     with custom configurations and dependencies.
/// </summary>
public class AppBuilder
{
    private readonly Func<CommandLineApplication> _appBuildFunc;
    private readonly List<Action<AppConstructionContainer>> _configurationActions = new();
    private readonly Action<IConfigurationBuilder>? _configurationBuilderAction;
    private readonly List<Action<IConfigurationBuilder>?> _configurationBuilderActions = new();

    private AppBuilder(Func<CommandLineApplication>? appBuildFunc,
                       Action<IConfigurationBuilder>? configurationBuilderAction,
                       params Action<AppConstructionContainer>[] configurationActions)
    {
        _configurationBuilderAction = configurationBuilderAction;

        SetupDefaults();

        _appBuildFunc = appBuildFunc ?? (() => new CommandLineApplication());
        foreach (var configurationAction in configurationActions)
        {
            _configurationActions.Add(configurationAction);
        }
    }

    /// <summary>
    ///     Creates a default instance of the <see cref="AppBuilder" /> with optional configuration
    ///     and command-line arguments.
    /// </summary>
    /// <param name="name">The application name (or title) shown during execution and in the help screens.</param>
    /// <param name="description">The application description displayed during execution and in the help screens.</param>
    /// <param name="configurationAction">
    ///     An optional action to apply additional configuration settings to the <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="args">
    ///     An array of command-line arguments to be added to the configuration.
    /// </param>
    /// <returns>
    ///     Returns an instance of <see cref="AppBuilder" /> with the default setup and the supplied configuration.
    /// </returns>
    public static AppBuilder CreateDefault(string name, string description, Action<IConfigurationBuilder>? configurationAction = null, params string[] args)
    {
        return CreateDefault(new CommandAppProperties(name, description), configurationAction, args);
    }

    /// <summary>
    ///     Creates a default instance of the <see cref="AppBuilder" /> with optional configuration
    ///     and command-line arguments.
    /// </summary>
    /// <param name="commandAppProperties">Properties of the command app, like name or description.</param>
    /// <param name="configurationAction">
    ///     An optional action to apply additional configuration settings to the <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="args">
    ///     An array of command-line arguments to be added to the configuration.
    /// </param>
    /// <returns>
    ///     Returns an instance of <see cref="AppBuilder" /> with the default setup and the supplied configuration.
    /// </returns>
    public static AppBuilder CreateDefault(CommandAppProperties commandAppProperties, Action<IConfigurationBuilder>? configurationAction = null, params string[] args)
    {
        var builder = new AppBuilder(() => new CommandLineApplication { Name = commandAppProperties.Name, Description = commandAppProperties.Description },
                                     null,
                                     container => container.Application.Conventions.UseDefaultConventions()
                                         .UseCommandAttribute()
                                         .UseCommandNameFromModelType());

        return builder.WithConfiguration(configurationAction ?? (configurationBuilder => ConfigurationSetup.DefaultFileConfiguration(configurationBuilder)));
    }

    /// <summary>
    ///     Adds a configuration action to the <see cref="AppBuilder" /> to be executed during the build process.
    /// </summary>
    /// <param name="configurationAction">
    ///     An action that configures the <see cref="AppConstructionContainer" />. Use this to add services, set up the
    ///     application,
    ///     or configure other components.
    /// </param>
    /// <returns>
    ///     Returns the instance of <see cref="AppBuilder" />, allowing for method chaining.
    /// </returns>
    public AppBuilder Configure(Action<AppConstructionContainer> configurationAction)
    {
        _configurationActions.Add(configurationAction);

        return this;
    }

    /// <summary>
    ///     Constructs and configures a <see cref="CommandLineApplication" /> instance with all specified
    ///     configurations and dependencies.
    /// </summary>
    /// <returns>
    ///     Returns a fully constructed and configured instance of <see cref="CommandLineApplication" />.
    /// </returns>
    public CommandLineApplication BuildConsole()
    {
        var app = _appBuildFunc();
        var serviceCollections = new ServiceCollection();

        var configurationBuilder = new ConfigurationBuilder();

        _configurationBuilderActions.Add(_configurationBuilderAction);

        foreach (var configurationBuilderAction in _configurationBuilderActions)
        {
            configurationBuilderAction?.Invoke(configurationBuilder);
        }

        var configuration = configurationBuilder.Build();

        var configurationContainer = new AppConstructionContainer(app, serviceCollections, configuration);

        foreach (var configurationAction in _configurationActions)
        {
            configurationAction(configurationContainer);
        }

        var serviceProviderFactory = configurationContainer.ServiceProviderFactory ?? (() => configurationContainer.Services.BuildServiceProvider());

        app.Conventions.UseConstructorInjection(serviceProviderFactory());

        return app;
    }

    private void SetupDefaults()
    {
        _configurationBuilderActions.Add(builder => ConfigurationSetup.DefaultFileConfiguration(builder));
    }

    private AppBuilder WithConfiguration(Action<IConfigurationBuilder>? configurationBuilderAction)
    {
        _configurationBuilderActions.Add(configurationBuilderAction);

        return this;
    }
}