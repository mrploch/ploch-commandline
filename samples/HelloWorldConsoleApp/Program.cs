// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

var app = AppBuilder.CreateDefault().Build();
app.Command<HellowWorldCommand>();

[Command(Name = "HelloWorld", Description = "Prints Hello World")]
public class HellowWorldCommand : IAsyncCommand
{
    [Option(Description = "Person First Name", ShortName = "f")]
    [Required]
    public required string FirstName { get; set; }

    [Option(Description = "Person Last Name", ShortName = "l")]
    [Required]
    public required string LastName { get; set; }

    [Option(Description = "Person Age", ShortName = "a")]
    [Required]
    public required int Age { get; set; }

    public Task OnExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{FirstName} {LastName} ({Age}) - Hello World!");

        return Task.CompletedTask;
    }
}