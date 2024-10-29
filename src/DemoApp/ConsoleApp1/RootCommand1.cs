using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

namespace ConsoleApp1;

[Command(Name = "command1")]
public class RootCommand1(CommandLineApplication app) : HelpOnlyCommand(app)
{
    [Option(Inherited = true)]
    public bool Verbose { get; set; }

    [Option(Inherited = true)]
    public string? Colour { get; set; }
}