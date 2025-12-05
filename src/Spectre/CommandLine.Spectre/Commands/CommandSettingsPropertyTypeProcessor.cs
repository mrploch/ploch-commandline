using System.Reflection;
using Ploch.Common.ArgumentChecking;
using Ploch.Common.Linq;
using Ploch.Common.Reflection;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Commands;

public abstract class CommandSettingsPropertyTypeProcessor<TProperty> : ICommandSettingsProcessor
{
    public Type SupportedPropertyType => typeof(TProperty);

    public virtual Type[] RequiredAttributes { get; } = [];

    protected IDictionary<string, OwnedPropertyInfo<TProperty>> Properties { get; } = new Dictionary<string, OwnedPropertyInfo<TProperty>>();

    public void ProcessArguments(CommandSettings arguments)
    {
        var properties = arguments.GetProperties<TProperty>();
        foreach (var property in properties)
        {
            if (RequiredAttributes.Any() && !RequiredAttributes.All(attributeType => property.GetCustomAttribute(attributeType) != null))
            {
                continue;
            }

            Properties.Add(property.Name, new(property, arguments));
        }

        DoProcessArguments();
    }

    protected static TAttribute GetAttribute<TAttribute>(PropertyInfo property, bool inherit = true) where TAttribute : Attribute =>
        property.GetCustomAttribute<TAttribute>(inherit).RequiredNotNull();

    protected abstract void DoProcessArguments();
}
