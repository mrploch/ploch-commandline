# Ploch.CommandLine.Spectre — Sample Application

A complete, runnable CLI built on the `Ploch.CommandLine.Spectre` packages. It exists to be read
and run: every feature of the library appears here in a form you can execute and compare against
the output below.

Building a CLI of your own? Start with the step-by-step
[Getting Started guide](../../docs/GETTING_STARTED.md) — it walks through the same features from an
empty project. This README is the tour of what is already here.

Packages exercised:

- `Ploch.CommandLine.Spectre` — `AppBuilder`, command base classes, `IOutput`, exit codes, tokens
- `Ploch.CommandLine.Spectre.FluentValidation` — settings validation
- `Ploch.CommandLine.Spectre.Serilog` — logging
- `Ploch.CommandLine.UseCases` — use cases with `Ardalis.Result`

## Build modes — read this first

The sample is deliberately **standalone**: it references the Ploch libraries as
`PackageReference`, exactly as an external consumer would, and it has its own
`Directory.Build.props` and `Directory.Packages.props` so it does not inherit anything from the
repository it lives in.

| Mode | Command | What it does |
|---|---|---|
| Standalone (default) | `dotnet build` | Restores `Ploch.*` from NuGet, as a consumer would |
| In-repository | `dotnet build -p:UsePlochProjectReferences=true` | Swaps those packages for `ProjectReference`s to the library sources in this repository |

**The default mode cannot restore yet.** The `Ploch.CommandLine.*` packages have not been published
(that is [issue #7](https://github.com/mrploch/ploch-commandline/issues/7)); until they are, a
plain `dotnet build` fails at restore. Use `-p:UsePlochProjectReferences=true`, which is also what
CI runs, so the sample cannot drift away from the libraries.

The switch lives in [`ProjectReferences.props`](ProjectReferences.props), imported conditionally by
[`Directory.Build.props`](Directory.Build.props).

## Running it

All commands below are run from the repository root.

```bash
dotnet build samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx -p:UsePlochProjectReferences=true
dotnet test  samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx -p:UsePlochProjectReferences=true
dotnet run --project samples/SampleApp/src/SampleApp -p:UsePlochProjectReferences=true -- --help
```

`dotnet run` passes everything after `--` to the application. The listings below drop the FIGlet
banner the application prints before each command, and were captured with `NO_COLOR=1`.

### The command tree

```text
$ sample --help

USAGE:
    sample [OPTIONS] <COMMAND>

EXAMPLES:
    sample info
    sample info -d
    sample user add Alice Smith -e alice@example.com -r Administrator
    sample user list
    sample user list -a -f compact

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    info       Display system, application, and host runtime information
    user       Manage user accounts and profile data
    config     Inspect and manage application configuration settings
    file       File processing and report generation utilities
    project    Project operations powered by Clean Architecture use cases and
               Ardalis.Result
```

Each branch has its own help — `sample user --help`, `sample config --help`, and so on.

### `info` — a synchronous command

```text
$ sample info

=== Application & System Information ===

╭──────────────────────┬────────────────────────────────────────────╮
│ Property             │ Value                                      │
├──────────────────────┼────────────────────────────────────────────┤
│ Application Name     │ Ploch.CommandLine.Spectre Sample App       │
│ Framework            │ .NET 10.0.11                               │
│ OS Description       │ Microsoft Windows 10.0.26200               │
│ Process Architecture │ X64                                        │
│ Current Directory    │ C:\DevNet\my\mrploch\ploch-commandline-wt9 │
│ Machine Name         │ KPLOCH-MSI                                 │
│ Environment Setting  │ Development                                │
╰──────────────────────┴────────────────────────────────────────────╯

Command completed successfully.
```

`info -d` adds a diagnostics panel. Implementation: `Commands/Common/InfoCommand.cs`, deriving from
`AppCommand<TSettings>`.

### `user` — asynchronous commands, DI and validation

```text
$ sample user list

Executing command UserListCommandSettings

Processing arguments...
Retrieving users (active only: False)...
                                Registered Users
╭────┬───────────────┬─────────────────┬───────────────┬──────────┬────────────╮
│ ID │ Name          │ Email           │ Role          │ Status   │ Created At │
├────┼───────────────┼─────────────────┼───────────────┼──────────┼────────────┤
│ 1  │ Alice Smith   │ alice.smith@exa │ Administrator │ Active   │ 2026-07-23 │
│    │               │ mple.com        │               │          │            │
│ 2  │ Bob Jones     │ bob.jones@examp │ Developer     │ Active   │ 2026-08-07 │
│    │               │ le.com          │               │          │            │
│ 3  │ Charlie Brown │ charlie.brown@e │ Contributor   │ Inactive │ 2026-08-17 │
│    │               │ xample.com      │               │          │            │
╰────┴───────────────┴─────────────────┴───────────────┴──────────┴────────────╯

Total users: 3
```

```text
$ sample user add "Alice Smith" -e alice@example.com -r Administrator

Creating new user account for Alice Smith...
╭─User Created Successfully────────╮
│ User ID: 4                       │
│ Name: Alice Smith                │
│ Email: alice@example.com         │
│ Role: Administrator              │
│ Active: Yes                      │
│ Created: 2026-08-22 12:37:59 UTC │
╰──────────────────────────────────╯
```

All three `user` commands inherit `GlobalSettings`, so `-v|--verbose` is declared once and shows up
in each of their help screens:

```text
$ sample user list -v -f compact -a

Verbose: format='compact', active only='True'.
Retrieving users (active only: True)...
[1] Alice Smith <alice.smith@example.com> (Administrator) - Active: True
[2] Bob Jones <bob.jones@example.com> (Developer) - Active: True

Total users: 2
```

FluentValidation rejects bad input before the command body runs:

```text
$ sample user add "A" -e "invalid-email"

Error: User name must be at least 2 characters long.
A valid email address must be provided.

[exit code -1]
```

A value the command itself rejects returns `ExitCode.InvalidInput` instead:

```text
$ sample user list -f xml

Unsupported format 'xml'. Supported formats: table, compact.

[exit code 2]
```

`user delete` shows `ILogger<T>` alongside `IOutput` — the console line is for the user, the log
entry for whoever reads the log afterwards:

```text
$ sample user delete 99

Deleting user with ID: 99 (force: False)...
User with ID 99 was not found.

[exit code 1]
```

```text
$ cat logs/sample.log
2026-08-22 14:36:16.690 +02:00 [WRN] [UserDeleteCommand] Delete requested for unknown user 99
```

### `config` — configuration

```text
$ sample config show

Application Configuration Settings

Configuration
├── SampleAppSettings
│   ├── DefaultOutputDirectory
│   │   └── Value: ./output
│   ├── EnableDiagnostics
│   │   └── Value: True
│   ├── Environment
│   │   └── Value: Development
│   └── MaxBatchSize
│       └── Value: 100
└── Serilog
    └── MinimumLevel
        ├── Default
        │   └── Value: Information
        └── Override
            ├── Microsoft
            │   └── Value: Warning
            └── System
                └── Value: Warning
```

```text
$ sample config get SampleAppSettings:Environment

SampleAppSettings:Environment: Development
```

`ConfigShowCommand` renders an **allow-list** of sections rather than `configuration.GetChildren()`.
The host adds an environment-variable provider, so enumerating the configuration root would print
every environment variable of the process — tokens and API keys included. The allow-list is the
point of the example.

`Program.cs` also pins the configuration base path to `AppContext.BaseDirectory`, so the settings
load no matter which directory the tool is invoked from.

### `file` — token expansion

```text
$ sample file process dataset.csv -o "./out-{date}/result.dat"

Processing File: dataset.csv
Resolved Output Path (with tokens replaced): ./out-2026-08-22/result.dat
Backup enabled: True

Processing file content...
File processed successfully!
Saved result to: ./out-2026-08-22/result.dat
```

`OutputPath` carries `[SupportsTokens]`; the `CommandArgumentsRootProcessor` rewrites `{date}`
before the command body sees the value. The simulated work honours the `CancellationToken`, so
Ctrl+C ends the command with `ExitCode.Cancelled` (130).

### `project` — use cases and `Ardalis.Result`

```text
$ sample project create MicroserviceDemo -d "Cloud native backend" -t WebAPI

Starting use case CreateProjectUseCase
Settings:
Name: MicroserviceDemo
Description: Cloud native backend
Template: WebAPI
Use case completed successfully.
```

```text
$ sample project create SpectreDemo

Starting use case CreateProjectUseCase
Settings:
Name: SpectreDemo
Description: Sample project
Template: Console
Use case failed: A project with name 'SpectreDemo' already exists.

[exit code 1]
```

`ProjectCreateCommand` derives from `UseCaseAsyncCommand<,,,>` and implements one method,
`CreateRequest`. Echoing the settings and rendering the result are inherited.

## Exit codes

| Code | Source | Meaning |
|---|---|---|
| 0 | `ExitCode.Success` | The command completed |
| 1 | `ExitCode.Error` | The command ran and failed |
| 2 | `ExitCode.InvalidInput` | The command rejected its input |
| 130 | `ExitCode.Cancelled` | Cancellation was requested (128 + SIGINT) |
| -1 | `Spectre.Console.Cli` | The command line could not be bound or validated |

## Development conveniences

`DEV_RUNTIME_CONSOLE_EXIT_PAUSE=true` makes the application wait for Enter before exiting — useful
when launching from an IDE.

## Layout

```
samples/SampleApp/
  Directory.Build.props                    # Standalone build settings; imports ProjectReferences.props on demand
  Directory.Packages.props                 # All package versions, as an external consumer would declare them
  ProjectReferences.props                  # In-repository mode: PackageReference -> ProjectReference
  Ploch.CommandLine.Spectre.SampleApp.slnx
  src/SampleApp/
    Program.cs                             # AppBuilder, DI, configuration, Serilog, command tree
    appsettings.json
    Commands/
      Common/                              # GlobalSettings, InfoCommand (AppCommand)
      Users/                               # AsyncAppCommand + FluentValidation validators
      Config/                              # IConfiguration-driven commands
      Files/                               # [SupportsTokens] and cancellation
      Projects/                            # UseCaseAsyncCommand + UseCases/
    Services/                              # In-memory domain services and models
  tests/SampleApp.Tests/
    Commands/                              # Command unit tests (mocked dependencies)
    Validation/                            # FluentValidation rule tests
```

## Tests

```bash
dotnet test samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx -p:UsePlochProjectReferences=true
```

20 tests: command exit codes, token expansion, cancellation handling, use case invocation and
validator rules. They use xUnit v3, FluentAssertions and Moq.
