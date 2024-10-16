using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

[Command(Name = "command1")]
public class RootCommand1(CommandLineApplication app)
{
    [Option(Inherited = true)]
    public bool Verbose { get; set; }
    
    [Option(Inherited = true)]
    public string? Colour { get; set; }
    
    // Inferred type = MultipleValues
    // Defined names = "-N"
    public void OnExecute()
    {
        app.HelpTextGenerator.Generate(app, app.Out);
        
        Console.WriteLine($"Command1 executed");
    }
}