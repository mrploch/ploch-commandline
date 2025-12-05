using System.Collections;
using System.Diagnostics;
using Ploch.Common;

namespace Ploch.CommandLine.Spectre;

public class EnvironmentSettingsLoader : IEnvironmentSettingsLoader
{
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
