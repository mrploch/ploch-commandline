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
    ///     Reads the environment settings: the debugger state, the pause-before-exit preference, and the
    ///     development-time runtime variables — those whose name starts with
    ///     <see cref="EnvironmentVariableNames.DevRuntimeVariablePrefix" />.
    /// </summary>
    /// <param name="target">The environment variable scope to read from. Defaults to the current process.</param>
    /// <returns>The environment settings read from the host environment.</returns>
    /// <remarks>
    ///     Only the <c>DEV_RUNTIME</c>-prefixed variables are captured. The full environment block is not
    ///     retained: it routinely carries secrets, and the caller can read it directly if it needs to.
    /// </remarks>
    public EnvironmentSettings Load(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var variables = Environment.GetEnvironmentVariables(target);

        // Ordinal-ignore-case: the Windows environment block can contain names differing only in case,
        // and an ordinal dictionary would then throw on the second one.
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry variable in variables)
        {
            var name = variable.Key.ToString();
            if (name is null || !name.StartsWith(EnvironmentVariableNames.DevRuntimeVariablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[name] = variable.Value?.ToString();
        }

        return new(Debugger.IsAttached, EnvironmentVariables.GetBool(EnvironmentVariableNames.PauseBeforeExit) ?? false, result.AsReadOnly());
    }
}
