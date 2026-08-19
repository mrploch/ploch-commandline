namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for the process-wide <see cref="EnvironmentSettings.Current" /> state: lazy initialisation is
///     synchronised, <see cref="EnvironmentSettings.Initialize" /> refuses to run once the settings have been
///     materialised (previously it no-opped silently), and <see cref="EnvironmentSettings.Reset" /> restores a
///     clean slate.
/// </summary>
[Collection(nameof(EnvironmentSettingsTests))]
public sealed class EnvironmentSettingsTests : IDisposable
{
    public void Dispose()
    {
        EnvironmentSettings.Reset();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Current_should_load_through_the_configured_loader()
    {
        EnvironmentSettings.Reset();
        var loader = new CountingLoader(new(false, true, new Dictionary<string, string?>()));
        EnvironmentSettings.Initialize(loader);

        EnvironmentSettings.Current.PauseBeforeExit.Should().BeTrue();
    }

    [Fact]
    public void Current_should_load_only_once()
    {
        EnvironmentSettings.Reset();
        var loader = new CountingLoader(new(false, false, new Dictionary<string, string?>()));
        EnvironmentSettings.Initialize(loader);

        _ = EnvironmentSettings.Current;
        _ = EnvironmentSettings.Current;
        _ = EnvironmentSettings.Current;

        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public void Current_should_load_exactly_once_under_concurrent_first_access()
    {
        EnvironmentSettings.Reset();
        var loader = new CountingLoader(new(false, false, new Dictionary<string, string?>()), delay: TimeSpan.FromMilliseconds(20));
        EnvironmentSettings.Initialize(loader);

        Parallel.For(0, 16, _ => EnvironmentSettings.Current.Should().NotBeNull());

        loader.LoadCount.Should().Be(1, "initialisation is synchronised, so a race must not load twice");
    }

    [Fact]
    public void Initialize_should_throw_when_the_settings_have_already_been_loaded()
    {
        EnvironmentSettings.Reset();
        _ = EnvironmentSettings.Current;

        var act = () => EnvironmentSettings.Initialize(new CountingLoader(new(false, false, new Dictionary<string, string?>())));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been loaded*", "a loader supplied too late would silently never be used");
    }

    [Fact]
    public void Reset_should_allow_a_loader_to_be_supplied_again()
    {
        EnvironmentSettings.Reset();
        _ = EnvironmentSettings.Current;

        EnvironmentSettings.Reset();
        var act = () => EnvironmentSettings.Initialize(new CountingLoader(new(false, false, new Dictionary<string, string?>())));

        act.Should().NotThrow();
    }

    [Fact]
    public void Current_should_be_replaceable_directly()
    {
        EnvironmentSettings.Reset();
        var replacement = new EnvironmentSettings(true, true, new Dictionary<string, string?> { ["DEV_RUNTIME_X"] = "1" });

        EnvironmentSettings.Current = replacement;

        EnvironmentSettings.Current.Should().BeSameAs(replacement);
        EnvironmentSettings.Current.IsDebugging.Should().BeTrue();
        EnvironmentSettings.Current.DevRuntimeVariables.Should().ContainKey("DEV_RUNTIME_X");
    }

    private sealed class CountingLoader(EnvironmentSettings settings, TimeSpan? delay = null) : IEnvironmentSettingsLoader
    {
        private int _loadCount;

        public int LoadCount => _loadCount;

        public EnvironmentSettings Load(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            Interlocked.Increment(ref _loadCount);

            if (delay is not null)
            {
                Thread.Sleep(delay.Value);
            }

            return settings;
        }
    }
}
