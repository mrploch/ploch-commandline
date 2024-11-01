﻿using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

namespace ConsoleApp1;

[Command(Name = "inner1")]
public class ChildCommand1(CommandLineApplication app, ISomeInterface someInterface, RootCommand1 parentCommand) : ICommand
{
    [Option]
    public string? SomeOption { get; set; }

    public void OnExecute()
    {
        someInterface.SomeMethod();
        Console.WriteLine($"{SomeOption}{parentCommand.Verbose}-{parentCommand.Colour}");

        Console.WriteLine("ChildCommand1 executed");

        Console.WriteLine();
        app.WriteHelpText();
    }
}