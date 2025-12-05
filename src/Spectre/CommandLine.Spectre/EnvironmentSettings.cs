namespace Ploch.CommandLine.Spectre;

public class EnvironmentSettings(bool isDebugging, bool pauseBeforeExit, IReadOnlyDictionary<string, string?> devRuntimeVariables)
{
    private static EnvironmentSettings? _current;
    private static IEnvironmentSettingsLoader _settingsLoader = new EnvironmentSettingsLoader();

    public static EnvironmentSettings Current
    {
        get
        {
            if (_current == null)
            {
                _current = _settingsLoader.Load();
            }

            return _current;
        }
        set => _current = value;
    }

    public bool IsDebugging { get; } = isDebugging;

    public bool PauseBeforeExit { get; } = pauseBeforeExit;

    public IReadOnlyDictionary<string, string?> DevRuntimeVariables { get; } = devRuntimeVariables;

    public static void Initialize(IEnvironmentSettingsLoader settingsLoader) => _settingsLoader = settingsLoader;
}
