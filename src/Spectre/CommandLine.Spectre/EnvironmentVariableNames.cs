namespace Ploch.CommandLine.Spectre;

public static class EnvironmentVariableNames
{
    public const string DevRuntimeVariablePrefix = "DEV_RUNTIME";
    public const string Separator = "_";
    public const string PauseBeforeExit = $"{DevRuntimeVariablePrefix}{Separator}CONSOLE_EXIT_PAUSE";
}
