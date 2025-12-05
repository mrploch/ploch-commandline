namespace Ploch.CommandLine.Spectre;

public interface IEnvironmentSettingsLoader
{
    EnvironmentSettings Load(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);
}
