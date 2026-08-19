namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for reading environment settings. Only <c>DEV_RUNTIME</c>-prefixed variables are captured: the
///     loader previously retained the entire environment block, which routinely carries secrets, and used
///     <c>Dictionary.Add</c> with an ordinal comparer so two names differing only in case threw.
/// </summary>
public sealed class EnvironmentSettingsLoaderTests : IDisposable
{
    private readonly List<string> _variablesToClear = [];

    public void Dispose()
    {
        foreach (var name in _variablesToClear)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Load_should_capture_dev_runtime_variables()
    {
        SetVariable("DEV_RUNTIME_SAMPLE_VALUE", "captured");

        var settings = new EnvironmentSettingsLoader().Load();

        settings.DevRuntimeVariables.Should().ContainKey("DEV_RUNTIME_SAMPLE_VALUE")
                .WhoseValue.Should().Be("captured");
    }

    [Fact]
    public void Load_should_not_capture_variables_outside_the_dev_runtime_prefix()
    {
        SetVariable("PLOCH_UNRELATED_VARIABLE", "should not be captured");

        var settings = new EnvironmentSettingsLoader().Load();

        settings.DevRuntimeVariables.Should().NotContainKey("PLOCH_UNRELATED_VARIABLE");
    }

    [Fact]
    public void Load_should_match_the_prefix_case_insensitively()
    {
        SetVariable("dev_runtime_lowercase_name", "value");

        var settings = new EnvironmentSettingsLoader().Load();

        settings.DevRuntimeVariables.Should().ContainKey("dev_runtime_lowercase_name");
    }

    [Fact]
    public void Load_should_expose_variables_case_insensitively()
    {
        SetVariable("DEV_RUNTIME_CASE_TEST", "value");

        var settings = new EnvironmentSettingsLoader().Load();

        settings.DevRuntimeVariables.Should().ContainKey("dev_runtime_case_test");
    }

    [Fact]
    public void Load_should_default_pause_before_exit_to_false()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableNames.PauseBeforeExit, null);

        var settings = new EnvironmentSettingsLoader().Load();

        settings.PauseBeforeExit.Should().BeFalse("pausing is a development convenience and must be opted into");
    }

    [Fact]
    public void Load_should_honour_the_pause_before_exit_variable_when_set()
    {
        SetVariable(EnvironmentVariableNames.PauseBeforeExit, "true");

        var settings = new EnvironmentSettingsLoader().Load();

        settings.PauseBeforeExit.Should().BeTrue();
    }

    [Fact]
    public void Load_should_not_throw_when_the_environment_is_read()
    {
        var act = () => new EnvironmentSettingsLoader().Load();

        act.Should().NotThrow("names differing only in case must not collide");
    }

    private void SetVariable(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);
        _variablesToClear.Add(name);
    }
}
