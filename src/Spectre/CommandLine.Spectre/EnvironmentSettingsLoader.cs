using System.Collections;
using System.Diagnostics;
using Ploch.Common;

namespace Ploch.CommandLine.Spectre;

/// <summary>
///     Reads <see cref="EnvironmentSettings" /> from the host environment variables.
/// </summary>
public class EnvironmentSettingsLoader : IEnvironmentSettingsLoader
{
    /// <summary>
    ///     Reads the environment settings, capturing the debugger state, the pause-before-exit preference,
    ///     and all environment variables in the specified scope.
    /// </summary>
    /// <param name="target">The environment variable scope to read from. Defaults to the current process.</param>
    /// <returns>The environment settings read from the host environment.</returns>
    public EnvironmentSettings Load(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var variables = Environment.GetEnvironmentVariables(target);

        var result = new Dictionary<string, string?>();

        foreach (DictionaryEntry variable in variables)
        {
            if (result.Keys is null)
            {
                continue;
            }

            result.Add(variable.Key.ToString()!, variable.Value?.ToString());
        }

        return new(Debugger.IsAttached, EnvironmentVariables.GetBool(EnvironmentVariableNames.PauseBeforeExit) ?? true, result.AsReadOnly());
    }
}
