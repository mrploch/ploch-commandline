namespace Ploch.CommandLine.Spectre.Tests.Testing;

/// <summary>
///     Groups the tests that mutate process-wide state — <c>AnsiConsole.Console</c>, <c>Console.In</c> and
///     <c>EnvironmentSettings.Current</c> — so they never run concurrently with one another.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalConsoleState
{
    public const string Name = "Global console state";

    private GlobalConsoleState()
    {
    }
}
