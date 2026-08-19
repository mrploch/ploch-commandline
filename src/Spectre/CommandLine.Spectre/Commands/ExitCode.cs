namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Exit codes returned by commands to the operating system.
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
    InvalidInput = 2,

    /// <summary>
    ///     Represents a command that was cancelled before completing. Matches the conventional shell
    ///     exit code for termination by SIGINT (128 + 2).
    /// </summary>
    Cancelled = 130
}
