using System.Numerics;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Represents information about a command.
/// </summary>
/// <param name="Name">The name of the command.</param>
/// <param name="Alias">An optional alias for the command.</param>
/// <param name="Description">An optional description of the command.</param>
/// <param name="IsHidden">Indicates whether the command is hidden from help listings.</param>
/// <param name="Examples">Optional examples of command usage.</param>
public record CommandInfo(string Name, string? Alias = null, string? Description = null, bool IsHidden = false, params IEnumerable<string> Examples)
    : IEqualityOperators<CommandInfo, CommandInfo, bool>;
