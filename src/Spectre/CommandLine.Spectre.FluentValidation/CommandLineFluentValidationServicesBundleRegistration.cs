using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Ploch.Common.Reflection;

namespace Ploch.CommandLine.Spectre.FluentValidation;

/// <summary>
///     Provides registration extensions that enable FluentValidation-based command settings validation.
/// </summary>
public static class CommandLineFluentValidationServicesBundleRegistration
{
    /// <summary>
    ///     Registers FluentValidation as the command settings validator and discovers validators from the
    ///     assemblies selected by <paramref name="validatorAssembliesBuilderAction" />.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="validatorAssembliesBuilderAction">A delegate selecting the assemblies scanned for validators.</param>
    /// <returns>The same <paramref name="services" /> instance, to allow chaining.</returns>
    public static IServiceCollection AddCommandLineSettingsFluentValidation(this IServiceCollection services,
                                                                            Action<AssemblyListBuilder> validatorAssembliesBuilderAction)
    {
        // The bundle already registers the ICommandSettingsValidator<> mapping. Registering it here too appended a
        // second, identical descriptor on every call, so IEnumerable<ICommandSettingsValidator<T>> resolved
        // duplicates. AddSingleton appends unconditionally; it does not de-duplicate.
        services.AddServicesBundle(CommandLineFluentValidationServicesBundle.Create(validatorAssembliesBuilderAction));

        return services;
    }
}
