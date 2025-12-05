namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Represents a successful execution of a command, indicating no errors occurred.
/// </summary>
public enum ExitCode
{
    /// <summary>
    ///     Represents a successful execution of a command.
    /// </summary>
    Success = 0,

    /// <summary>
    ///     Represents an error during the execution of a command.
    /// </summary>
    Error = 1,

    /// <summary>
    ///     Represents an invalid input provided to a command.
    /// </summary>
    InvalidInput = 2
}
