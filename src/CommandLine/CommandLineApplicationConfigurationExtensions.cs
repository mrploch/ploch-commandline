using System;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace Ploch.Common.CommandLine;

/// <summary>
///     Provides extension methods for configuring and enhancing the behavior of
///     <see cref="CommandLineApplication" /> instances.
/// </summary>
/// <remarks>
///     This class includes methods to add custom validators and configure commands for command-line applications.
/// </remarks>
public static class CommandLineApplicationConfigurationExtensions
{
    /// <summary>
    ///     Adds a custom validator to the specified
    ///     <see cref="CommandLineApplication{TModel}" /> instance.
    /// </summary>
    /// <typeparam name="TModel">
    ///     The type of the model associated with the
    ///     <see cref="CommandLineApplication{TModel}" />.
    /// </typeparam>
    /// <param name="application">
    ///     The <see cref="CommandLineApplication{TModel}" /> instance to which the
    ///     validator will be added.
    /// </param>
    /// <param name="validator">
    ///     A delegate representing the custom validation logic to be executed before the command is executed.
    /// </param>
    /// <returns>
    ///     The <see cref="CommandLineApplication{TModel}" /> instance with the added
    ///     validator.
    /// </returns>
    /// <remarks>
    ///     This method allows you to add a custom pre-execution validator to a command-line application.
    ///     The validator is executed before the command is executed, ensuring that the application meets
    ///     specific validation criteria.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         var app = new CommandLineApplication&ltMyModel&gt();
    ///         app.AddValidator((command, context) =>
    ///         {
    ///            // Custom validation logic
    ///            return ValidationResult.Success;
    ///          });
    /// </code>
    /// </example>
    public static CommandLineApplication<TModel> AddValidator<TModel>(this CommandLineApplication<TModel> application, PreExecuteCommandValidator validator)
        where TModel : class
    {
        application.Validators.Add(new DelegatedCommandValidator(validator));

        return application;
    }

    public static CommandLineApplication<TModel> Command<TModel>(this CommandLineApplication application,
                                                                 Action<CommandLineApplication<TModel>>? configuration = null)
        where TModel : class
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