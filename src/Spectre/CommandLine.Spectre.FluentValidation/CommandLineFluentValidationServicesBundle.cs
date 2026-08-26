using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Ploch.Common.Reflection;

namespace Ploch.CommandLine.Spectre.FluentValidation;

/// <summary>
///     A services bundle that configures FluentValidation for command line settings validation.
///     Registers validators from specified assemblies and configures the FluentCommandSettingsValidator.
/// </summary>
/// <param name="validatorAssemblies">A collection of assemblies containing FluentValidation validators to be registered.</param>
public class CommandLineFluentValidationServicesBundle(params IEnumerable<Assembly> validatorAssemblies) : ServicesBundle
{
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

        return new(builder.Build());
    }

    /// <summary>
    ///     Configures the service collection with FluentValidation validators and command settings validator.
    /// </summary>
    public override void DoConfigure()
    {
        // Validators keep FluentValidation's default scoped lifetime, so a consumer validator is free to depend on a
        // DbContext or any other scoped service. FluentCommandSettingsValidator<> is a singleton - Spectre resolves
        // commands from the root provider - but it resolves the validator inside a scope per validation rather than
        // injecting it, so the singleton never captures a scoped dependency.
        Services.AddValidatorsFromAssemblies(validatorAssemblies)
                .AddSingleton(typeof(ICommandSettingsValidator<>), typeof(FluentCommandSettingsValidator<>));
    }
}
