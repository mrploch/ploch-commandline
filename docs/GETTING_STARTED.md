# Getting Started with Ploch.CommandLine.Spectre

A hands-on walkthrough: you build a small CLI from an empty directory, one feature at a time, and
run it after every step. Every console listing below is real output captured from the finished
application, not an illustration.

For the conceptual overview — what each package is for, how the pieces fit together — see the
documentation site home page and the *Introduction* article. This guide is deliberately practical
and does not repeat them.

The finished application lives in [`samples/SampleApp`](../samples/SampleApp/README.md). If you
would rather read the code than type it, start there.

## Contents

1. [Prerequisites](#1-prerequisites)
2. [Create the project](#2-create-the-project)
3. [Bootstrap the application with AppBuilder](#3-bootstrap-the-application-with-appbuilder)
4. [Your first command: settings and AppCommand](#4-your-first-command-settings-and-appcommand)
5. [Configuration that survives the working directory](#5-configuration-that-survives-the-working-directory)
6. [Asynchronous commands and dependency injection](#6-asynchronous-commands-and-dependency-injection)
7. [Composing a multi-level CLI](#7-composing-a-multi-level-cli)
8. [Validating settings with FluentValidation](#8-validating-settings-with-fluentvalidation)
9. [Token expansion in settings](#9-token-expansion-in-settings)
10. [Use cases and Ardalis.Result](#10-use-cases-and-ardalisresult)
11. [Exit codes](#11-exit-codes)
12. [Cancellation](#12-cancellation)
13. [Logging with Serilog](#13-logging-with-serilog)
14. [Testing commands](#14-testing-commands)
15. [Development conveniences](#15-development-conveniences)

---

## 1. Prerequisites

- .NET SDK 10.0 or later (`dotnet --version`).
- A NuGet feed carrying the `Ploch.*` packages. They are published to the MrPloch GitHub Packages
  feed; see the repository README for the feed URL and authentication.

## 2. Create the project

```bash
dotnet new console -n MyTool
cd MyTool
dotnet add package Ploch.CommandLine.Spectre
dotnet add package Spectre.Console.Cli
```

Add the optional packages as you reach the steps that need them:

| Package | Adds |
|---|---|
| `Ploch.CommandLine.Spectre` | `AppBuilder`, the command base classes, `IOutput`, exit codes, token processing |
| `Ploch.CommandLine.Spectre.FluentValidation` | FluentValidation-backed settings validation |
| `Ploch.CommandLine.Spectre.Serilog` | Serilog wiring with console and rolling-file sinks |
| `Ploch.CommandLine.UseCases` | `IResultUseCase<,>` and `UseCaseAsyncCommand<,,,>` |

## 3. Bootstrap the application with AppBuilder

`AppBuilder` builds a `Microsoft.Extensions.Hosting` host, wires its service provider into
`Spectre.Console.Cli`, and returns an executor you run with the process arguments.

```csharp
using Ploch.CommandLine.Spectre;

var executor = AppBuilder.Create(args)
                         .WithName("My Tool")
                         .WithVersion(new Version(1, 0, 0))
                         .WithDescription("Does something useful.")
                         .ConfigureCommandApp(config =>
                         {
                             config.SetApplicationName("mytool");
                         });

return executor.Run(args);
```

Three things happen here that you do not have to write yourself:

- **Application banner.** The name is rendered as FIGlet text, followed by the version and
  description, before any command runs.
- **Hosting.** `Host.CreateDefaultBuilder` supplies configuration, logging and the service
  provider. Anything you register with `ConfigureServices` is injectable into commands.
- **Ctrl+C.** `AppBuilder.Create` installs a `Console.CancelKeyPress` handler that cancels a
  `CancellationTokenSource` instead of killing the process, which is what the token your commands
  receive is linked to (see [Cancellation](#12-cancellation)).

`ConfigureServices` has two overloads — one taking just `IServiceCollection`, one taking the
`HostBuilderContext` as well, which is how you reach `IConfiguration` during registration:

```csharp
.ConfigureServices((context, services) =>
{
    services.AddSingleton<IUserService, UserService>();
    services.Configure<MyOptions>(context.Configuration.GetSection("MyOptions"));
})
```

## 4. Your first command: settings and AppCommand

A command is a pair: a **settings** class describing the command line, and a **command** class
doing the work.

Settings derive from Spectre's `CommandSettings` and use its attributes. `[CommandArgument]` is
positional (`<REQUIRED>` in angle brackets, `[OPTIONAL]` in square ones); `[CommandOption]` is a
named flag; `[Description]` feeds the generated help; `[DefaultValue]` supplies the default and is
also shown in help.

```csharp
using System.ComponentModel;
using Spectre.Console.Cli;

public class InfoCommandSettings : CommandSettings
{
    [CommandOption("-d|--diagnostics")]
    [Description("Display extended runtime and host diagnostics.")]
    [DefaultValue(false)]
    public bool ShowDiagnostics { get; set; }
}
```

For synchronous work, derive from `AppCommand<TSettings>` and implement `DoExecute`:

```csharp
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

public class InfoCommand(ICommandSettingsValidator<InfoCommandSettings> validator,
                         IExceptionHandler exceptionHandler,
                         IOutput output) : AppCommand<InfoCommandSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(CommandContext? context, InfoCommandSettings settings, CancellationToken cancellationToken)
    {
        output.MarkupLineInterpolated($"[bold cyan]Hello from My Tool[/]");

        return ExitCode.Success;
    }
}
```

Note what the base class gives you and what you therefore never write in `DoExecute`:

- **Validation** runs first, through the injected `ICommandSettingsValidator<TSettings>`.
- **Exceptions** never escape. Anything thrown goes to the injected `IExceptionHandler`, whose
  return value becomes the exit code.
- **Cancellation** is separated from failure: an `OperationCanceledException` is not treated as a
  fault, it returns `ExitCode.Cancelled`.
- **`ExitCode`, not `int`.** The base class casts for you.

`AppCommand<TSettings>` has no `Output` property — use the `IOutput` you injected. Its asynchronous
sibling, introduced in step 6, does expose `Output`.

Register the command and run it:

```csharp
config.AddCommand<InfoCommand>("info")
      .WithDescription("Display system, application, and host runtime information.")
      .WithExample("info")
      .WithExample("info", "-d");
```

```text
$ mytool info

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

The `WithExample` calls are not decoration — they are what fills the `EXAMPLES:` block of the
generated help.

## 5. Configuration that survives the working directory

Add an `appsettings.json` and copy it to the output directory:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

There is a trap here that only shows up once you install the tool. The host resolves relative
configuration paths against the **current working directory**, and a CLI is run from wherever the
user happens to be — so `appsettings.json` silently fails to load and every setting reads back as
`null`. Anchor it to the deployment directory instead:

```csharp
.ConfigureAppConfiguration(configuration => configuration.SetBasePath(AppContext.BaseDirectory)
                                                         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
```

`optional: false` is deliberate: a missing configuration file should fail loudly at start-up rather
than produce a tool that behaves differently depending on the directory it was launched from.

Inject `IConfiguration` into a command like any other service.

**Do not enumerate the configuration root for display.** The host adds an environment-variable
provider, so `configuration.GetChildren()` yields every environment variable of the process — API
keys and access tokens included. If a command renders configuration, give it an allow-list of the
sections your application owns:

```csharp
private static readonly string[] ApplicationSections = ["SampleAppSettings", "Logging", "Serilog"];
```

## 6. Asynchronous commands and dependency injection

`AsyncAppCommand<TSettings>` is the asynchronous base class. It takes two more dependencies than
`AppCommand<TSettings>` — a `CommandArgumentsRootProcessor` (which pre-processes settings, e.g.
token expansion) and an `IOutput`, exposed to you as the `Output` property.

```csharp
public class UserAddCommand(CommandArgumentsRootProcessor settingsProcessor,
                            ICommandSettingsValidator<UserAddCommandSettings> validator,
                            IExceptionHandler exceptionHandler,
                            IOutput output,
                            IUserService userService)
    : AsyncAppCommand<UserAddCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override async Task<ExitCode> DoExecuteAsync(CommandContext context,
                                                           UserAddCommandSettings settings,
                                                           CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[cyan]Creating new user account for[/] [bold yellow]{settings.Name}[/]...");

        var user = await userService.CreateUserAsync(settings.Name, settings.Email, settings.Role, cancellationToken);

        return ExitCode.Success;
    }
}
```

Use the inherited `Output` property rather than capturing the constructor parameter — capturing a
parameter that is also passed to the base constructor stores it twice and the compiler warns about
it (CS9107).

`IUserService` is resolved from the container you configured in step 3; command constructors are
plain constructor injection.

```text
$ mytool user add "Alice Smith" -e alice@example.com -r Administrator

Executing command UserAddCommandSettings

Processing arguments...
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

The `Executing command …` / `Processing arguments…` preamble comes from `AsyncAppCommand`, not from
the command body.

## 7. Composing a multi-level CLI

Sub-commands are grouped into **branches**. A branch is a verb with no behaviour of its own that
owns a set of commands, and branches can be nested to any depth.

```csharp
var executor = appBuilder.ConfigureCommandApp(config =>
{
    config.SetApplicationName("sample");

    // Root-level command.
    config.AddCommand<InfoCommand>("info")
          .WithDescription("Display system, application, and host runtime information.");

    // A branch with three sub-commands.
    config.AddBranch("user", user =>
    {
        user.SetDescription("Manage user accounts and profile data.");

        user.AddCommand<UserAddCommand>("add")
            .WithDescription("Create a new user account with validation.")
            .WithExample("user", "add", "Alice Smith", "-e", "alice@example.com", "-r", "Administrator");

        user.AddCommand<UserListCommand>("list")
            .WithDescription("List registered user accounts in a rich table.");

        user.AddCommand<UserDeleteCommand>("delete")
            .WithDescription("Delete a user account by ID.");
    });
});
```

Help is generated for every level. The root:

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

And the branch:

```text
$ sample user --help

DESCRIPTION:
Manage user accounts and profile data

USAGE:
    sample user [OPTIONS] <COMMAND>

EXAMPLES:
    sample user add Alice Smith -e alice@example.com -r Administrator
    sample user list
    sample user list -a -f compact
    sample user delete 1 --force

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <NAME>     Create a new user account with validation
    list           List registered user accounts in a rich table
    delete <ID>    Delete a user account by ID
```

Nesting further is the same call again — `user.AddBranch("keys", keys => …)` gives you
`sample user keys rotate`.

### Options shared by a group of commands

Declare an option once in a base settings class and inherit it, rather than repeating the property
on every command in a branch:

```csharp
public class GlobalSettings : CommandSettings
{
    [CommandOption("-v|--verbose")]
    [Description("Enable verbose console output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
}

public class UserListCommandSettings : GlobalSettings
{
    [CommandOption("-a|--active-only")]
    [Description("Only display active user accounts.")]
    [DefaultValue(false)]
    public bool ActiveOnly { get; set; }
}
```

Inherited options appear in the generated help of every derived command, defaults included:

```text
$ sample user list --help

OPTIONS:
                             DEFAULT
    -h, --help                          Prints help information
    -v, --verbose                       Enable verbose console output
    -a, --active-only                   Only display active user accounts
    -f, --format <FORMAT>    table      The output format: 'table' or 'compact'
```

An option that appears in help must do something in every command that inherits it — a flag the
command silently ignores is worse than no flag.

## 8. Validating settings with FluentValidation

Add the package, then register validation once during service configuration. Assembly scanning
picks up every `AbstractValidator<TSettings>` in the assemblies you list:

```csharp
using Ploch.CommandLine.Spectre.FluentValidation;

services.AddCommandLineSettingsFluentValidation(builder => builder.AddAssembly(typeof(Program).Assembly));
```

This registers `FluentCommandSettingsValidator<T>` as the `ICommandSettingsValidator<T>` your
commands receive, so no command code changes.

```csharp
public class UserAddCommandSettingsValidator : AbstractValidator<UserAddCommandSettings>
{
    public UserAddCommandSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("User name is required.")
            .MinimumLength(2).WithMessage("User name must be at least 2 characters long.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid email address must be provided.");
    }
}
```

Validation runs before `DoExecuteAsync`, so an invalid invocation never reaches your code:

```text
$ sample user add "A" -e "invalid-email"

Error: User name must be at least 2 characters long.
A valid email address must be provided.
```

**Mind the exit code.** A failed `Validate` is reported by `Spectre.Console.Cli` itself, which
short-circuits with its own exit code of `-1` (`255` as the shell sees it) — *not* with
`ExitCode.InvalidInput`. `ExitCode.InvalidInput` is what your command returns when it rejects input
itself; see [Exit codes](#11-exit-codes).

## 9. Token expansion in settings

Mark a string setting with `[SupportsTokens]` and the `CommandArgumentsRootProcessor` rewrites its
value before `DoExecuteAsync` runs. `{date}` and `{datetime}` are resolved out of the box.

```csharp
[CommandOption("-o|--output-path <PATH>")]
[Description("Output destination path. Supports tokens like '{date}' and '{datetime}'.")]
[SupportsTokens]
[DefaultValue("./processed-{date}/output.dat")]
public string OutputPath { get; set; } = "./processed-{date}/output.dat";
```

The command body sees the expanded value, never the template:

```text
$ sample file process dataset.csv -o "./out-{date}/result.dat"

Executing command FileProcessCommandSettings

Processing arguments...
Processing File: dataset.csv
Resolved Output Path (with tokens replaced): ./out-2026-08-22/result.dat
Backup enabled: True

Processing file content...
File processed successfully!
Saved result to: ./out-2026-08-22/result.dat
```

Token expansion only happens for commands whose base class runs the settings processor — the
asynchronous ones. `AppCommand<TSettings>` does not take a processor.

## 10. Use cases and Ardalis.Result

When the real work belongs in an application layer rather than in the CLI, put it in an
`IResultUseCase<TRequest, TResponse>` and let `UseCaseAsyncCommand<,,,>` do the plumbing.

```csharp
public class CreateProjectUseCase(IProjectRepository projectRepository)
    : IResultUseCase<CreateProjectRequest, CreateProjectResponse>
{
    public async Task<Result<CreateProjectResponse>> ExecuteAsync(CreateProjectRequest request,
                                                                  CancellationToken cancellationToken = default)
    {
        var existing = await projectRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            return Result<CreateProjectResponse>.Conflict($"A project with name '{request.Name}' already exists.");
        }

        var project = new ProjectItem(request.Name, request.Description, request.Template, DateTime.UtcNow);
        await projectRepository.AddAsync(project, cancellationToken);

        return Result<CreateProjectResponse>.Success(new(project.Name, project.Description, project.Template, project.CreatedAt));
    }
}
```

The command shrinks to a single mapping method — everything else, including rendering the result,
is inherited:

```csharp
public class ProjectCreateCommand(IOutput output,
                                  CreateProjectUseCase useCase,
                                  CommandArgumentsRootProcessor settingsProcessor,
                                  ICommandSettingsValidator<ProjectCreateCommandSettings> validator,
                                  IExceptionHandler exceptionHandler)
    : UseCaseAsyncCommand<ProjectCreateCommandSettings, CreateProjectUseCase, CreateProjectRequest, CreateProjectResponse>(
        output, useCase, settingsProcessor, validator, exceptionHandler)
{
    protected override CreateProjectRequest CreateRequest(ProjectCreateCommandSettings commandSettings) =>
        new(commandSettings.Name, commandSettings.Description, commandSettings.Template);
}
```

Success and failure are rendered by the base class, which returns `ExitCode.Success` and
`ExitCode.Error` respectively. Override `ProcessSuccessResponse` or `ProcessFailureResponse` to
change either.

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

## 11. Exit codes

`ExitCode` is the contract between your commands and whatever script calls them.

| Member | Value | Meaning |
|---|---|---|
| `Success` | 0 | The command completed. |
| `Error` | 1 | The command ran and failed. |
| `InvalidInput` | 2 | The command rejected the input it was given. |
| `Cancelled` | 130 | The command stopped because cancellation was requested (128 + SIGINT). |

Two exit codes do **not** come from this enumeration:

- **`-1`** — `Spectre.Console.Cli` could not bind or validate the command line (unknown command,
  missing required argument, failed `Validate`). It is produced before your command runs.
- **Whatever `IExceptionHandler` returns** — an unhandled exception inside a command is routed to
  the handler, and the handler's return value is the exit code.

Return `InvalidInput` from a command when the input parses fine but is not acceptable — a value
outside a supported set, a file that does not exist, mutually exclusive flags:

```csharp
if (!SupportedFormats.Contains(settings.Format, StringComparer.OrdinalIgnoreCase))
{
    Output.MarkupLineInterpolated($"[red]Unsupported format '{settings.Format}'. Supported formats: {string.Join(", ", SupportedFormats)}.[/]");

    return ExitCode.InvalidInput;
}
```

```text
$ sample user list -f xml
Unsupported format 'xml'. Supported formats: table, compact.

$ echo $LASTEXITCODE
2
```

## 12. Cancellation

Every `DoExecute` / `DoExecuteAsync` receives a `CancellationToken`. Forward it — into service
calls, `Task.Delay`, HTTP requests, database queries. A command that accepts the token and ignores
it cannot be interrupted.

```csharp
protected override async Task<ExitCode> DoExecuteAsync(CommandContext context,
                                                       FileProcessCommandSettings settings,
                                                       CancellationToken cancellationToken)
{
    await AnsiConsole.Status()
                     .Spinner(Spinner.Known.Dots)
                     .StartAsync("Processing file content...",
                                 async _ => await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken));

    return ExitCode.Success;
}
```

The base class treats cancellation as an outcome rather than a fault: an `OperationCanceledException`
is not passed to `IExceptionHandler`, it becomes `ExitCode.Cancelled` (130).

## 13. Logging with Serilog

`Ploch.CommandLine.Spectre.Serilog` configures Serilog as the logging provider, with a console sink
and two rolling files — everything, and errors and warnings only:

```csharp
using Ploch.CommandLine.Spectre.Serilog;

.ConfigureServices((context, services) =>
{
    services.AddSerilog(context.Configuration,
                        logName: "sample",
                        logPath: Path.Combine(AppContext.BaseDirectory, "logs"));
})
```

Levels come from the `Serilog` section of `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Warning", "System": "Warning" }
    }
  }
}
```

Inject `ILogger<TCommand>` into a command and use it for the operator's record, keeping `IOutput`
for what the user reads:

```csharp
logger.LogWarning("[UserDeleteCommand] Delete requested for unknown user {UserId}", settings.Id);
Output.MarkupLineInterpolated($"[red]User with ID {settings.Id} was not found.[/]");
```

```text
$ cat logs/sample.log
2026-08-22 14:36:16.690 +02:00 [WRN] [UserDeleteCommand] Delete requested for unknown user 99

$ cat logs/sample-errors.log
[14:36:16 WRN] [Ploch.CommandLine.Spectre.SampleApp.Commands.Users.UserDeleteCommand] [UserDeleteCommand] Delete requested for unknown user 99
```

## 14. Testing commands

Commands are ordinary classes with constructor dependencies, so they are tested without a host.
Construct the command with test doubles, build a `CommandContext`, and call the public
`Execute` / `ExecuteAsync` — that path exercises validation, exception handling and cancellation as
well as your `DoExecute` body.

```csharp
public class UserListCommandTests
{
    private readonly Mock<ICommandSettingsValidator<UserListCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IOutput> _outputMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly CommandArgumentsRootProcessor _processor = new([]);

    [Fact]
    public async Task ExecuteAsync_should_return_invalid_input_when_the_format_is_not_supported()
    {
        var settings = new UserListCommandSettings { Format = "xml" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "list", null);

        var command = new UserListCommand(_processor,
                                          _validatorMock.Object,
                                          _exceptionHandlerMock.Object,
                                          _outputMock.Object,
                                          _userServiceMock.Object);

        var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput);
    }
}
```

Two things worth testing that are easy to overlook:

- **Token expansion.** Give the processor a `TokensArgumentsProcessor`
  (`new CommandArgumentsRootProcessor([new TokensArgumentsProcessor()])`) and assert the setting was
  rewritten.
- **Cancellation.** Pass an already-cancelled token and assert `ExitCode.Cancelled`, plus that the
  exception handler was never called — that proves cancellation is not being reported as a failure.

## 15. Development conveniences

Set `DEV_RUNTIME_CONSOLE_EXIT_PAUSE=true` and the application waits for Enter before exiting, so a
console window launched from an IDE does not close before you can read it. It is read from the
environment, so it never affects a build server or an end user who has not set it.

Environment variables prefixed `DEV_RUNTIME` are collected into `EnvironmentSettings.Current`
alongside the debugger state.

---

## Running the sample

```bash
# From the repository root, against the library sources in this repository:
dotnet run --project samples/SampleApp/src/SampleApp -p:UsePlochProjectReferences=true -- --help
dotnet test samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx -p:UsePlochProjectReferences=true
```

See [`samples/SampleApp/README.md`](../samples/SampleApp/README.md) for the full command tour and
for the difference between the standalone (NuGet) and in-repository (project reference) build
modes.
