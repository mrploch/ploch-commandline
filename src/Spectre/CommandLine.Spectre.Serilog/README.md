# Ploch.CommandLine.Spectre.Serilog

[Serilog](https://serilog.net/) integration for
[Ploch.CommandLine.Spectre](https://www.nuget.org/packages/Ploch.CommandLine.Spectre): one call configures
Serilog as the application's `ILogger` provider. It writes a size-rolled log file, a separate file for
warnings and errors, and console output through Spectre.Console, and reads any further Serilog settings
from the application's configuration.

## Installation

```bash
dotnet add package Ploch.CommandLine.Spectre.Serilog
```

## Example

```csharp
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.Serilog;

using var appBuilder = AppBuilder.Create(args)
                                 .ConfigureServices((context, services) =>
                                     services.AddSerilog(context.Configuration,
                                                         logName: "my-tool",
                                                         logPath: Path.Combine(AppContext.BaseDirectory, "logs")));

// Register your commands with appBuilder.ConfigureCommandApp(...) and run them as usual.
```

Log files are written with the invariant culture by default; pass `culture:` to override it, and
`template:` to change the output template.

## Documentation

- [Getting Started guide](https://github.com/mrploch/ploch-commandline/blob/main/docs/GETTING_STARTED.md)
- [Documentation site](https://commandline.github.ploch.dev/)
