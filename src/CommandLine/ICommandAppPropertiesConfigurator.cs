namespace Ploch.Common.CommandLine;

public interface ICommandAppPropertiesConfigurator
{
    ICommandAppPropertiesConfigurator WithName(string name);

    ICommandAppPropertiesConfigurator WithDescription(string description);
}

public interface ICommandAppPropertiesBuilder : ICommandAppPropertiesConfigurator
{
    CommandAppProperties Build();
}