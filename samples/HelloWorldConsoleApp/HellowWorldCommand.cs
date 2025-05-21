using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;
#pragma warning disable Ex0100

namespace HelloWorldConsoleApp;

[Command(Name = "HelloWorld", Description = "Prints Hello World")]
#pragma warning disable ClassDocumentationHeader
public class HellowWorldCommand : IAsyncCommand
#pragma warning restore ClassDocumentationHeader
#pragma warning disable MethodDocumentationHeader
#pragma warning disable PropertyDocumentationHeader
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
#pragma warning restore PropertyDocumentationHeader