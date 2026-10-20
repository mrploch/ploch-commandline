namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Attribute that describes a command in the command-line application.
///     It carries the metadata that <see cref="CommandInfoFactory" /> reads into a <see cref="CommandInfo" />;
///     nothing registers a command from it.
/// </summary>
/// <param name="name">The primary name of the command used to invoke it from the command line.</param>
/// <param name="alias">An optional alternative name (alias) for the command.</param>
/// <param name="description">An optional description of the command that explains its purpose.</param>
/// <param name="examples">Optional examples demonstrating how to use the command.</param>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class CommandAttribute(string name, string? alias = null, string? description = null, params string[] examples) : Attribute
{
    /// <summary>
    ///     Gets the primary name of the command.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Gets the optional alias for the command.
    /// </summary>
    public string? Alias { get; } = alias;

    /// <summary>
    ///     Gets the optional description of the command.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    ///     Gets the collection of examples showing how to use the command.
    /// </summary>
    public IEnumerable<string> Examples { get; } = examples;

    /// <summary>
    ///     Gets or sets whether the command should be hidden from command listings.
    /// </summary>
    /// <value><c>true</c> if the command should be hidden; otherwise, <c>false</c>. Default is <c>false</c>.</value>
    public bool IsHidden { get; set; }
}
