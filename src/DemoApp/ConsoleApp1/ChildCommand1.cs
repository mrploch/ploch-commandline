using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

[Command(Name = "inner1")]
public class ChildCommand1(ISomeInterface someInterface, RootCommand1 parentCommand) : ICommand
{
    [Option]
    public string? SomeOption { get; set; }

    public void OnExecute()
    {
        someInterface.SomeMethod();
        Console.WriteLine($"{SomeOption}{parentCommand.Verbose}-{parentCommand.Colour}");
    }
}