# Ploch CommandLine Applications

<!-- @import "[TOC]" {cmd="toc" depthFrom=1 depthTo=6 orderedList=false} -->

<!-- code_chunk_output -->

- [Ploch CommandLine Applications](#ploch-commandline-applications)
    - [Overview](#overview)
    - [Quick Start](#quick-start)

<!-- /code_chunk_output -->

## Overview

Ploch CommandLine Applications is an opinionated library for building CommandLine applications in .NET Core.
It extensions the [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) library providing following features.

- Prescribed way for configuring Dependency Injection using
    - Microsoft.Extensions.DependencyInjection, or
    - Autofac
- Preconfigured Microsoft Extensions Configuration
- Preconfigured Serilog logging
- Simple, single-line, building of a configured and executable app
- Interfaces for building commands
- Simplified configuration for verbs (commands), including nested verbs

## Quick Start

1. Create a console application project

2. Create a command class which includes executable code and commandline options:

```csharp
using McMaster.Extensions.CommandLineUtils;
using Ploch.Common.CommandLine;

[Command(Name = "command1")]
public class RootCommand1(ISomeInterface dependency)
{
    [Option]
    public bool Verbose { get; set; }
    
    [Option]
    public string? Colour { get; set; }
    
    // Inferred type = MultipleValues
    // Defined names = "-N"
    public void OnExecute()
    {       
        Console.WriteLine($"Command1 executed");
    }
}
```

3. Update the `Program.cs` file:

```csharp
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.CommandLine;
using Ploch.Common.CommandLine.Autofac;

// Demonstrates verbs with verbs using commands
await AppBuilder.CreateDefault(cfg => cfg.AddJsonFile("appsettings.json"))
    .Configure(container =>
    {
        // Add services
        container.ServiceCollection.AddSingleton<RootCommand1>()
                                   .AddSingleton<ISomeInterface, SomeClass>();

        container.Application.Command<RootCommand1>();
    })
    .Build()
    .ExecuteAsync(args);

```

4. Run:

```powershell
myapp.exe --help
myapp.exe command1 --verbose --colour Pink
```
