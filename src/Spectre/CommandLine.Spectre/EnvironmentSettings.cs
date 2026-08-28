namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Describes the environment the console application is running in.
/// </summary>
/// <param name="isDebugging">A value indicating whether the application is running under a debugger.</param>
/// <param name="pauseBeforeExit">A value indicating whether the application should wait for input before exiting.</param>
/// <param name="devRuntimeVariables">The development-time runtime variables available to the application.</param>
public class EnvironmentSettings(bool isDebugging, bool pauseBeforeExit, IReadOnlyDictionary<string, string?> devRuntimeVariables)
{
    private static readonly object SyncRoot = new();

    // volatile because the getter reads this outside the lock. Without it the unsynchronised fast path is a broken
    // double-checked lock: another thread could observe a non-null reference before the writes that initialised the
    // instance are visible. Benign on x86, not on ARM64.
    private static volatile EnvironmentSettings? _current;
    private static IEnvironmentSettingsLoader _settingsLoader = new EnvironmentSettingsLoader();

    /// <summary>
    ///     Gets or sets the current environment settings, loading them on first access via the configured loader.
    /// </summary>
    /// <remarks>
    ///     Initialisation is synchronised, so concurrent first access loads the settings exactly once.
    /// </remarks>
    public static EnvironmentSettings Current
    {
        get
        {
            if (_current is not null)
            {
                return _current;
            }

            lock (SyncRoot)
            {
                return _current ??= _settingsLoader.Load();
            }
        }

        set
        {
            lock (SyncRoot)
            {
                _current = value;
            }
        }
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
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="Current" /> has already been materialised, because the supplied loader would
    ///     otherwise silently never be used.
    /// </exception>
    public static void Initialize(IEnvironmentSettingsLoader settingsLoader)
    {
        lock (SyncRoot)
        {
            if (_current is not null)
            {
                throw new InvalidOperationException(
                    "EnvironmentSettings.Current has already been loaded; Initialize must be called before the first read.");
            }

            _settingsLoader = settingsLoader;
        }
    }

    /// <summary>
    ///     Clears the cached settings and restores the default loader. Intended for test isolation, since
    ///     <see cref="Current" /> is process-wide state.
    /// </summary>
    public static void Reset()
    {
        lock (SyncRoot)
        {
            _current = null;
            _settingsLoader = new EnvironmentSettingsLoader();
        }
    }
}
