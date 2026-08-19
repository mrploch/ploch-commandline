using System.Reflection;
using Ploch.Common.ArgumentChecking;
using Ploch.Common.Linq;
using Ploch.Common.Reflection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Base class for processors that handle command settings properties of a specific type.
/// </summary>
/// <typeparam name="TProperty">The property type this processor handles.</typeparam>
public abstract class CommandSettingsPropertyTypeProcessor<TProperty> : ICommandSettingsProcessor
{
    /// <summary>
    ///     Gets the property type handled by this processor.
    /// </summary>
    public Type SupportedPropertyType => typeof(TProperty);

    /// <summary>
    ///     Gets the attribute types a property must carry to be processed. An empty array means no attributes are required.
    /// </summary>
    public virtual Type[] RequiredAttributes { get; } = [];

    /// <summary>
    ///     Gets the matched properties, keyed by property name, collected during <see cref="ProcessArguments" />.
    /// </summary>
    protected IDictionary<string, OwnedPropertyInfo<TProperty>> Properties { get; } = new Dictionary<string, OwnedPropertyInfo<TProperty>>();

    /// <summary>
    ///     Collects every property of type <typeparamref name="TProperty" /> that carries all
    ///     <see cref="RequiredAttributes" />, then invokes <see cref="DoProcessArguments" />.
    /// </summary>
    /// <param name="arguments">The command settings whose properties are inspected.</param>
    public void ProcessArguments(CommandSettings arguments)
    {
        var properties = arguments.GetProperties<TProperty>();
        foreach (var property in properties)
        {
            if (RequiredAttributes.Length != 0 && !RequiredAttributes.All(attributeType => property.GetCustomAttribute(attributeType) != null))
            {
                continue;
            }

            Properties.Add(property.Name, new(property, arguments));
        }

        DoProcessArguments();
    }

    /// <summary>
    ///     Retrieves a required attribute from a property.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to retrieve.</typeparam>
    /// <param name="property">The property to read the attribute from.</param>
    /// <param name="inherit">Whether to search the property's inheritance chain. Defaults to <see langword="true" />.</param>
    /// <returns>The attribute found on the property.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the attribute is not present on the property.</exception>
    protected static TAttribute GetAttribute<TAttribute>(PropertyInfo property, bool inherit = true) where TAttribute : Attribute =>
        property.GetCustomAttribute<TAttribute>(inherit).RequiredNotNull();

    /// <summary>
    ///     Processes the properties collected in <see cref="Properties" />.
    /// </summary>
    protected abstract void DoProcessArguments();
}
