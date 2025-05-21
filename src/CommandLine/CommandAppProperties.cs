namespace Ploch.Common.CommandLine;

public record CommandAppProperties(string Name, string Description);

public class CommandAppPropertiesBuilder
{
    private string _description = string.Empty;
    private string _name = string.Empty;

    public CommandAppPropertiesBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CommandAppPropertiesBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CommandAppProperties Build()
    {
        return new CommandAppProperties(_name, _description);
    }
}