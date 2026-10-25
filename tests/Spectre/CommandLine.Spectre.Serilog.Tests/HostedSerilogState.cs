namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Groups the tests that build a host through <c>AppBuilder</c> so they never run concurrently with one another
///     or with the rest of the assembly: the hosting registration of Serilog assigns the process-wide
///     <c>Log.Logger</c>, the run swaps <c>AnsiConsole.Console</c>, and the builder's console-driven cancellation
///     handling is process-wide state as well.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HostedSerilogState
{
    public const string Name = "Hosted Serilog state";

    private HostedSerilogState()
    {
    }
}
