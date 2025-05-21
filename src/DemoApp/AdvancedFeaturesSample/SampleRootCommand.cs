using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

namespace BasicConsoleApp;

[Command(Name = "command1")]
public class SampleRootCommand(CommandLineApplication app) : HelpOnlyCommand(app)
{
    [Option(Inherited = true)]
    public bool Verbose { get; set; }

    [Option(Inherited = true)]
    public string? Colour { get; set; }
}