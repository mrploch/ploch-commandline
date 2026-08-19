namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Describes the environment the console application is running in.
/// </summary>
/// <param name="isDebugging">A value indicating whether the application is running under a debugger.</param>
/// <param name="pauseBeforeExit">A value indicating whether the application should wait for input before exiting.</param>
/// <param name="devRuntimeVariables">The development-time runtime variables available to the application.</param>
public class EnvironmentSettings(bool isDebugging, bool pauseBeforeExit, IReadOnlyDictionary<string, string?> devRuntimeVariables)
{
    private static EnvironmentSettings? _current;
    private static IEnvironmentSettingsLoader _settingsLoader = new EnvironmentSettingsLoader();

    /// <summary>
    ///     Gets or sets the current environment settings, loading them on first access via the configured loader.
    /// </summary>
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

    /// <summary>
    ///     Gets a value indicating whether the application is running under a debugger.
    /// </summary>
    public bool IsDebugging { get; } = isDebugging;

    /// <summary>
    ///     Gets a value indicating whether the application should wait for input before exiting.
    /// </summary>
    public bool PauseBeforeExit { get; } = pauseBeforeExit;

    /// <summary>
    ///     Gets the development-time runtime variables available to the application.
    /// </summary>
    public IReadOnlyDictionary<string, string?> DevRuntimeVariables { get; } = devRuntimeVariables;

    /// <summary>
    ///     Replaces the loader used to populate <see cref="Current" />. Call before <see cref="Current" /> is first read.
    /// </summary>
    /// <param name="settingsLoader">The loader to use when reading environment settings.</param>
    public static void Initialize(IEnvironmentSettingsLoader settingsLoader) => _settingsLoader = settingsLoader;
}
