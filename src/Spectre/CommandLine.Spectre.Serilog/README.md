# Ploch.CommandLine.Spectre.Serilog

[Serilog](https://serilog.net/) integration for
[Ploch.CommandLine.Spectre](https://www.nuget.org/packages/Ploch.CommandLine.Spectre): one call configures
Serilog as the application's `ILogger` provider. It writes a size-rolled log file, a separate file for
warnings and above, and console output through Spectre.Console, and reads any further Serilog settings
from the application's configuration.

## Installation

Serilog is opt-in: the core package registers no logging provider, so an application that wants Serilog
adds this package and calls `UseSerilog` on its `AppBuilder`, once. This package depends on
`Ploch.CommandLine.Spectre`.

```bash
dotnet add package Ploch.CommandLine.Spectre.Serilog
```

## Example

```csharp
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.Serilog;

using var appBuilder = AppBuilder.Create(args)
                                 .UseSerilog(logName: "my-tool",
                                             logPath: Path.Combine(AppContext.BaseDirectory, "logs"));

// Register your commands with appBuilder.ConfigureCommandApp(...) and run them as usual.
```

`UseSerilog` reads the `Serilog` section of the host configuration for the minimum level and
overrides. Log files are written with the invariant culture by default; pass `culture:` to override
it, and `template:` to change the output template. `services.AddSerilog(configuration, ...)` on an
`IServiceCollection` is the same registration for code that configures services directly; use one
or the other, not both.

## Documentation

- [Getting Started guide](https://github.com/mrploch/ploch-commandline/blob/main/docs/GETTING_STARTED.md)
- [Documentation site](https://commandline.github.ploch.dev/)
