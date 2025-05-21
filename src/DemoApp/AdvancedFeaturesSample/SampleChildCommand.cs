using BasicConsoleApp;
using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

namespace AdvancedFeaturesSample;

/// <summary>
///     This is a sub-command for the <see cref="SampleRootCommand" /> command.
/// </summary>
/// <param name="app">The <see cref="CommandLineApplication" /> instance.</param>
/// <param name="sampleService">An interface that is injected here using the dependency injection.</param>
/// <param name="parentCommand">Parent command allowing access to its options.</param>
[Command(Name = "inner1")]
public class SampleChildCommand(CommandLineApplication app, ISampleService sampleService, SampleRootCommand parentCommand) : ICommand
{
    [Option]
    public string? SomeOption { get; set; }

    public void OnExecute()
    {
        sampleService.ExecuteSomeAction();
        Console.WriteLine($"{SomeOption}{parentCommand.Verbose}-{parentCommand.Colour}");

        Console.WriteLine("ChildCommand1 executed");

        Console.WriteLine();
        app.WriteHelpText();
    }
}