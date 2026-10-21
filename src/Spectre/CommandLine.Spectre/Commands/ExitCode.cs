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
    ///     Represents an error during the execution of a command. Also the exit code of a failure that happened
    ///     before the application's host existed, such as an exception thrown while building it.
    /// </summary>
    Error = 1,

    /// <summary>
    ///     Represents an invalid input provided to a command. Returned by a command that rejects the input it was given,
    ///     and by the application when Spectre.Console.Cli could not parse the command line, could not bind the settings,
    ///     or the settings failed validation; see <see cref="AppBuilder.ConfigureCommandApp" />.
    /// </summary>
    InvalidInput = 2,

    /// <summary>
    ///     Represents a command that was cancelled before completing. Matches the conventional shell
    ///     exit code for termination by SIGINT (128 + 2).
    /// </summary>
    Cancelled = 130
}
