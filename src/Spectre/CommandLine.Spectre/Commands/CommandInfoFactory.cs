using System.Reflection;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Factory class for creating <see cref="CommandInfo" /> instances from command types.
/// </summary>
public static class CommandInfoFactory
{
    /// <summary>
    ///     Creates a <see cref="CommandInfo" /> instance from the specified command type.
    /// </summary>
    /// <param name="commandType">
    ///     The type of the command to create information for.
    ///     This type may contain a <see cref="CommandAttribute" /> that provides additional command metadata.
    /// </param>
    /// <returns>
    ///     A <see cref="CommandInfo" /> instance containing the command's metadata.
    ///     If no <see cref="CommandAttribute" /> is found, returns a command info with the type name as the command name.
    /// </returns>
    public static CommandInfo CreateFromType(Type commandType)
    {
        var commandAttributes = commandType.GetCustomAttribute<CommandAttribute>(false);

        if (commandAttributes == null)
        {
            return new(commandType.Name);
        }

        return new(commandAttributes.Name,
                   commandAttributes.Alias,
                   commandAttributes.Description,
                   commandAttributes.IsHidden,
                   commandAttributes.Examples.ToArray());
    }
}
