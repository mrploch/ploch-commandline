using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.Common.DependencyInjection;
using Ploch.Common.Reflection;

namespace PlochCommandLine.Spectre.FluentValidation;

public static class CommandLineFluentValidationServicesBundleRegistration
{
    public static IServiceCollection AddCommandLineSettingsFluentValidation(this IServiceCollection services,
                                                                            Action<AssemblyListBuilder> validatorAssembliesBuilderAction)
    {
        services.AddSingleton(typeof(ICommandSettingsValidator<>), typeof(FluentCommandSettingsValidator<>))
                .AddServicesBundle(CommandLineFluentValidationServicesBundle.Create(validatorAssembliesBuilderAction));

        return services;
    }
}
