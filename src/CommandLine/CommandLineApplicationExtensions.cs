using System;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine
{
    public static class CommandLineApplicationExtensions
    {
        public static CommandLineApplication<TModel> AddValidator<TModel>(this CommandLineApplication<TModel> application, PreExecuteCommandValidator validator)
            where TModel : class
        {
            application.Validators.Add(new DelegatedCommandValidator(validator));

            return application;
        }

        public static CommandLineApplication<TModel> Command<TModel>(this CommandLineApplication application,
                                                                     Action<CommandLineApplication<TModel>>? configuration = null) where TModel : class
        {
            var commandType = typeof(TModel);

            var commandAttribute = commandType.GetCustomAttribute<CommandAttribute>(false);

            if (commandAttribute == null)
            {
                throw new InvalidOperationException($"Type {commandType.Name} is not decorated with {nameof(CommandAttribute)} attribute.");
            }

            if (string.IsNullOrEmpty(commandAttribute.Name))
            {
                throw new
                    InvalidOperationException($"{nameof(CommandAttribute)} on type {commandType.Name} has to have a non-null or empty {nameof(CommandAttribute.Name)} property");
            }

            return application.Command(commandAttribute.Name!, configuration!);
        }
    }
}