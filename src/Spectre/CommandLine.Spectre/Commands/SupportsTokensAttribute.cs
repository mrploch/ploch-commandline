namespace Ploch.CommandLine.Spectre.Commands;

[AttributeUsage(AttributeTargets.Property)]
public class SupportsTokensAttribute(bool pathSafe = true) : Attribute
{
    public bool PathSafe => pathSafe;
}
