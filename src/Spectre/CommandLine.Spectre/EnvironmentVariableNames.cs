namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Names of the environment variables recognised by the console application host.
/// </summary>
public static class EnvironmentVariableNames
{
    /// <summary>
    ///     The prefix identifying environment variables that carry development-time runtime settings.
    /// </summary>
    public const string DevRuntimeVariablePrefix = "DEV_RUNTIME";

    /// <summary>
    ///     The separator used between segments of an environment variable name.
    /// </summary>
    public const string Separator = "_";

    /// <summary>
    ///     The environment variable controlling whether the application waits for input before exiting.
    /// </summary>
    public const string PauseBeforeExit = $"{DevRuntimeVariablePrefix}{Separator}CONSOLE_EXIT_PAUSE";
}
