namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Defines a contract for reading <see cref="EnvironmentSettings" /> from the host environment.
/// </summary>
public interface IEnvironmentSettingsLoader
{
    /// <summary>
    ///     Reads the environment settings from the specified environment variable scope.
    /// </summary>
    /// <param name="target">The environment variable scope to read from. Defaults to the current process.</param>
    /// <returns>The environment settings read from the host environment.</returns>
    EnvironmentSettings Load(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);
}
