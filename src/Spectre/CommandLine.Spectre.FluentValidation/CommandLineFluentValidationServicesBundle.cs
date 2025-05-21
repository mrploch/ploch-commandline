using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Ploch.Common.Reflection;

namespace PlochCommandLine.Spectre.FluentValidation;

/// <summary>
///     A services bundle that configures FluentValidation for command line settings validation.
///     Registers validators from specified assemblies and configures the FluentCommandSettingsValidator.
/// </summary>
/// <param name="validatorAssemblies">A collection of assemblies containing FluentValidation validators to be registered.</param>
public class CommandLineFluentValidationServicesBundle(params IEnumerable<Assembly> validatorAssemblies) : IServicesBundle
{
    /// <summary>
    ///     Configures the service collection with FluentValidation validators and command settings validator.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public void Configure(IServiceCollection services)
    {
        services.AddValidatorsFromAssemblies(validatorAssemblies).AddSingleton(typeof(ICommandSettingsValidator<>), typeof(FluentCommandSettingsValidator<>));
    }

    /// <summary>
    ///     Creates a new instance of CommandLineFluentValidationServicesBundle using an assembly list builder.
    /// </summary>
    /// <param name="validatorAssembliesBuilderAction">
    ///     An action to configure the assembly list builder for specifying
    ///     validator assemblies.
    /// </param>
    /// <returns>A new instance of CommandLineFluentValidationServicesBundle with the configured validator assemblies.</returns>
    public static CommandLineFluentValidationServicesBundle Create(Action<AssemblyListBuilder> validatorAssembliesBuilderAction)
    {
        var builder = new AssemblyListBuilder();
        validatorAssembliesBuilderAction(builder);

        return new CommandLineFluentValidationServicesBundle(builder.Build());
    }
}
