using Ploch.Common.CommandLine;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace HostingSample;
public class HelloRootCommand(CommandLineApplication app) : HelpOnlyCommand(app)
{
    [Option(Inherited = true)]
    public bool Verbose { get; set; }

    [Option(Inherited = true)]
    [Required]
    public string HelloText { get; set; } = null!;
}