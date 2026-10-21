# Getting Started with Ploch.CommandLine.Spectre

A hands-on walkthrough: you build a small CLI from an empty directory, one feature at a time, and
run it after every step. Every console listing below is real output captured from the finished
application rather than an illustration, with machine-specific values — paths and host names —
replaced by neutral placeholders.

For the conceptual overview — what each package is for, how the pieces fit together — see the
documentation site home page and the *Introduction* article. This guide is deliberately practical
and does not repeat them.

The finished application lives in [`samples/SampleApp`](https://github.com/mrploch/ploch-commandline/blob/main/samples/SampleApp/README.md). If you
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

`Ploch.CommandLine.Spectre` is the only Ploch package you need to start. It depends on
`Ploch.CommandLine.Spectre.Serilog` and registers its `SerilogConfigurationBundle` through
`AppServicesBundle`, so logging goes through Serilog from the first run;
[section 13](#13-logging-with-serilog) shows how to configure it. Add the remaining packages as
you reach the steps that need them:

| Package | Adds |
|---|---|
| `Ploch.CommandLine.Spectre` | `AppBuilder`, the command base classes, `IOutput`, exit codes, token processing |
| `Ploch.CommandLine.Spectre.FluentValidation` | FluentValidation-backed settings validation |
| `Ploch.CommandLine.Spectre.Serilog` | Serilog wiring with the Spectre.Console console sink and two rolling log files — installed with the core package |
| `Ploch.CommandLine.UseCases` | `IResultUseCase<,>` and `UseCaseAsyncCommand<,,,>` |

## 3. Bootstrap the application with AppBuilder

`AppBuilder` builds a `Microsoft.Extensions.Hosting` host, wires its service provider into
`Spectre.Console.Cli`, and returns an executor you run with the process arguments.

```csharp
using Ploch.CommandLine.Spectre;

// The builder owns the Ctrl+C handler and the cancellation source it creates, so dispose it once
// the run has returned. The token stays live for the whole run, so an earlier scope exit would
// cancel the application it is meant to be shutting down.
using var appBuilder = AppBuilder.Create(args)
                                 .WithName("My Tool")
                                 .WithVersion(new Version(1, 0, 0))
                                 .WithDescription("Does something useful.");

var executor = appBuilder.ConfigureCommandApp(config =>
{
    config.SetApplicationName("mytool");
});

return executor.Run(args);
```

Three things happen here that you do not have to write yourself:

- **Application banner.** The name is rendered as FIGlet text, followed by the version and
  description, before any command runs — on standard error, so it never lands in a pipe.
  `ShowBanner(false)` drops it, together with the `Executing command …` line, for a tool whose
  standard error is captured too.
- **Hosting.** `Host.CreateDefaultBuilder` supplies configuration, logging and the service
  provider. Anything you register with `ConfigureServices` is injectable into commands. The host
  is built once the run reaches command resolution (a parse failure or `--version` returns before
  that) and the library never starts it: a CLI command is the unit of work, so an `IHostedService`
  you register (`services.AddHostedService<T>()`) has neither `StartAsync` nor `StopAsync` called
  by the library, and a host that was built is disposed when the run ends. Starting the host around
  the command is a possible 1.x feature, tracked as
  [PLO-785](https://linear.app/ploch/issue/PLO-785); say so there if you need it.
- **Ctrl+C.** `AppBuilder.Create` installs a `Console.CancelKeyPress` handler over a
  `CancellationTokenSource`, and that source's token is the one your commands receive (see
  [Cancellation](#12-cancellation)). The **first** interrupt cancels it cooperatively instead of
  killing the process, so a command that honours its token can stop and tidy up; a **second**
  interrupt takes the default path and terminates, so a command that ignores its token never leaves
  the application unkillable from the keyboard. The builder owns both the source and the handler:
  disposing it unsubscribes the handler and releases the source, and the handler also detaches itself
  once an interrupt has been handled. `Console.CancelKeyPress` is process-wide, so a builder that is
  neither disposed nor interrupted keeps its handler installed for the life of the process.

The banner and the `Executing command …` line that precedes each command no longer touch standard
output: both go to standard error, so
`mytool user list | cat` (or `> users.txt`) carries only what the command rendered through
`IOutput`, while the terminal still shows the banner. Redirect `2>/dev/null` (`2>nul` in `cmd`,
`2>$null` in PowerShell) to hide them, or call
`ShowBanner(false)` to drop both altogether. (The Ctrl+C notice and the `PauseBeforeExit` prompt still use standard output;
[PLO-819](https://linear.app/ploch/issue/PLO-819) tracks them.) The listings in this guide show
standard output only.

`ConfigureServices` has two overloads — one taking just `IServiceCollection`, one taking the
`HostBuilderContext` as well, which is how you reach `IConfiguration` during registration:

```csharp
.ConfigureServices((context, services) =>
{
    services.AddSingleton<IUserService, UserService>();
    services.Configure<MyOptions>(context.Configuration.GetSection("MyOptions"));
})
```

Both overloads are additive, and so are `ConfigureHost` and `ConfigureAppConfiguration`: call them
as many times as you like and every delegate runs, in the order you added them. That is the same
behaviour as the `IHostBuilder` methods underneath, so registration can be split across helper
methods without one call quietly replacing another.

The three methods share **one** sequence, replayed in call order when the application is built, so
the chain reads the way it runs. When two calls register the same service, the later call wins,
whether it used `ConfigureServices` or `host.ConfigureServices` inside `ConfigureHost`. The same
goes for application configuration sources that supply the same key, whether added with
`ConfigureAppConfiguration` or `host.ConfigureAppConfiguration`:

```csharp
AppBuilder.Create(args)
          .ConfigureHost(host => host.ConfigureServices(s => s.AddSingleton<IClock, FakeClock>()))
          .ConfigureServices(s => s.AddSingleton<IClock, SystemClock>()); // IClock resolves to SystemClock
```

Swap the two calls and `IClock` resolves to `FakeClock`.

Call order decides precedence between calls of the same kind only, not which phase of the host
build a delegate runs in. As with any `IHostBuilder`, host configuration
(`ConfigureHostConfiguration`) runs first, then application configuration, then services, then
`ConfigureContainer`. So an application configuration source always overrides a host
configuration source for the same key, whatever order you called them in.

The builder's own defaults sit outside that sequence. The standard host sources —
`appsettings.json`, `appsettings.{Environment}.json`, user secrets in Development, environment
variables and the command-line arguments, each overriding the one before — are loaded before any
source you add, and the services bundles are configured before any service you register, so you
can override both. The application's `CancellationTokenSource` is registered after every service
delegate, so no `ConfigureServices` call can replace it. Your commands always receive the
builder's own token either way.

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

public class InfoCommand(CommandArgumentsRootProcessor settingsProcessor,
                         ICommandSettingsValidator<InfoCommandSettings> validator,
                         IExceptionHandler exceptionHandler,
                         IOutput output) : AppCommand<InfoCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override ExitCode DoExecute(CommandContext? context, InfoCommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[bold cyan]Hello from My Tool[/]");

        return ExitCode.Success;
    }
}
```

Note what the base class gives you and what you therefore never write in `DoExecute`:

- **Validation** runs first, through the injected `ICommandSettingsValidator<TSettings>`.
- **Settings processing** runs before `DoExecute`: the injected `CommandArgumentsRootProcessor`
  passes the settings through every registered processor (token expansion, for example — see
  step 9).
- **Exceptions** never escape. Anything thrown goes to the injected `IExceptionHandler`, whose
  return value becomes the exit code.
- **Cancellation** is separated from failure: when the token the command received has been
  cancelled, an `OperationCanceledException` is not treated as a fault, it returns
  `ExitCode.Cancelled`. One raised while that token is still live is a fault like any other.
- **`ExitCode`, not `int`.** The base class casts for you.

Before `DoExecute` runs, the application announces the command on standard error with a short
`Executing command info` line, named after the command's registered name (an alias is announced
by the name it is an alias of). The base class itself writes
nothing to the injected `IOutput`, so standard output is yours.

Render through the inherited `Output` property rather than the constructor parameter — capturing a
parameter that is also passed to the base constructor stores it twice and the compiler warns about
it (CS9107). The asynchronous sibling, introduced in step 6, takes the same dependencies.

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
│ Current Directory    │ C:\projects\sample-app                     │
│ Machine Name         │ EXAMPLE-HOST                               │
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
configuration paths against its **content root**, which defaults to the current working directory,
and a CLI is run from wherever the user happens to be — so `appsettings.json` silently fails to load
and every setting that only that file supplies reads back as `null`. Nothing tells you: the source is
optional, and the environment variables and the command line still work, so the tool half-works.

The fix is to move the content root, not to add a second source:

```csharp
.ConfigureHost(host => host.UseContentRoot(AppContext.BaseDirectory))
```

That one call anchors the host's own `appsettings.json` **and** `appsettings.{Environment}.json`
lookups to the directory the application was deployed to, and changes nothing else.

**Do not re-add `appsettings.json` yourself to reach that directory.** The obvious-looking

```csharp
// Wrong: fixes the path, breaks the precedence.
.ConfigureAppConfiguration(configuration => configuration.SetBasePath(AppContext.BaseDirectory)
                                                         .AddJsonFile("appsettings.json", optional: false))
```

does find the file, but configuration sources are ordered and the last one added wins. The host has
already layered `appsettings.json`, `appsettings.{Environment}.json`, user secrets (in the
`Development` environment only), the environment variables and the command-line arguments, in
ascending precedence. Appending the file puts it back
on **top** of all of them, so `--MySection:BatchSize=5` is silently overridden by the value in the
file it was meant to override. `AppBuilder` itself carried exactly this bug until it was fixed in
[#82](https://github.com/mrploch/ploch-commandline/issues/82).

`UseContentRoot` costs you one thing: the host's own source is `optional: true`, so a missing file no
longer throws. If you want the old fail-loud behaviour — and you probably do, since a tool that
behaves differently depending on the directory it was launched from is worse than one that refuses to
start — check for the file explicitly instead of relying on `optional: false`:

```csharp
.ConfigureHost(host =>
{
    var settingsFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(settingsFile))
    {
        throw new FileNotFoundException("The application settings file was not found.", settingsFile);
    }

    host.UseContentRoot(AppContext.BaseDirectory);
})
```

The sample application packages both halves as a single `UseSettingsFromDeploymentDirectory`
extension method on `IHostBuilder`; see `samples/SampleApp/src/SampleApp/HostBuilderExtensions.cs`.

Inject `IConfiguration` into a command like any other service.

**Do not enumerate the configuration root for display.** The host adds an environment-variable
provider, so `configuration.GetChildren()` yields every environment variable of the process — API
keys and access tokens included. If a command renders configuration, give it an allow-list of the
sections your application owns:

```csharp
private static readonly string[] ApplicationSections = ["SampleAppSettings", "Logging", "Serilog"];
```

## 6. Asynchronous commands and dependency injection

`AsyncAppCommand<TSettings>` is the asynchronous base class. It takes the same dependencies as
`AppCommand<TSettings>` — a `CommandArgumentsRootProcessor` (which pre-processes settings, e.g.
token expansion), the validator, the exception handler and an `IOutput`, exposed to you as the
`Output` property — and does the same work around your implementation.

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

Commands and their settings are transient: Spectre resolves a fresh instance for each run, so a
command holds a transient service for its own lifetime rather than the host's. The settings processor
the library registers is transient as well, so its state is not shared between runs. A command that
constructor-injects its own settings type receives a fresh, unbound instance; the bound settings are
the argument Spectre passes to `Execute`. Services you register keep the lifetime you give them,
and singleton services stay shared between commands. Anything that must be shared between
resolutions belongs in a singleton service the command depends on, not in the command itself — a
command type cannot be re-registered as a singleton from `ConfigureServices`, because Spectre's own
registration is queued later and wins.

```text
$ mytool user add "Alice Smith" -e alice@example.com -r Administrator
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

The `Executing command add` line that precedes this on the terminal comes from the application,
on standard error, not from the command body; `AsyncAppCommand` and `AppCommand` write nothing of
their own to the output.

### Render through IOutput, and escape what the user typed

Two habits worth forming early, both visible throughout the sample.

**Render renderables through `IOutput`, not the static `AnsiConsole`.** `IOutput.Write(IRenderable)`
takes a `Table`, `Panel` or `Tree` just as `AnsiConsole.Write` does, but it goes through the console
the host configured and can be mocked in a test:

```csharp
Output.Write(table);        // not AnsiConsole.Write(table)
```

**Escape values that came from outside your source file.** Table cells, panel content, tree nodes
and `Markup` are all parsed as Spectre markup, so a user name, path or configuration value
containing `[` throws during rendering or injects formatting:

```csharp
table.AddRow(user.Id.ToString(), Markup.Escape(user.Name), Markup.Escape(user.Email));
```

`MarkupLineInterpolated` escapes its interpolation holes automatically — that is the difference
between it and building a `Markup` from an interpolated string, which does not.

**`Write` and `WriteLine` print a string as-is; markup is an explicit opt-in.** This mirrors the
split Spectre.Console makes between `IAnsiConsole.Write(string)` and `IAnsiConsole.Markup(string)`:

```csharp
Output.WriteLine(@"C:\logs[1].txt");            // prints C:\logs[1].txt - the bracket is text
Output.MarkupLine("[green]Done.[/]");           // parsed as markup: a green "Done."
Output.MarkupLineInterpolated($"[green]{path}[/]"); // markup around an escaped value
```

Because `Markup` and `MarkupLine` parse everything they are given, a value that did not originate
in your source file goes through `Markup.Escape(...)` first, or through the `…Interpolated` overloads,
which escape it for you.

**`IOutput.Write` dispatches on the type of the message.** An `IRenderable` is rendered by the
console; everything else is offered to the registered `IMessageWriter`s, and the writer whose message
type is the most derived match receives the message itself along with the
`IMessageFormatterProcessor`, so the writer decides how the value is formatted. A `string` is written
verbatim by `StringMessageWriter`, a `FormattableString` is rendered as interpolated markup by
`FormattableStringMessageWriter`, writing an `Exception` renders the full exception through
`ExceptionMessageWriter`, and writing a collection produces one line per item. Only a message no
writer accepts falls back to being written as text, through `IFormattable.ToString` with the
caller's provider where the type supports it.

Register your own with `services.AddMessageWriter<TMessage, TWriter>()` — and format the message
inside `Write` rather than expecting to be handed formatted text. Of two writers for the same type
the later registration wins, and the built-in bundle registers before your `ConfigureServices`
delegates, so registering a writer for `string` or `FormattableString` replaces the built-in one and
changes how `Write` and `WriteLine` render that type. A writer takes the `IAnsiConsole` it renders
to, not `IOutput`, which depends on the writers:

```csharp
public sealed class QuotingStringWriter(IAnsiConsole console) : TypeMessageWriter<string>
{
    public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
        console.Write($"\"{message}\"");
}

AppBuilder.Create(args)
          .ConfigureServices(services => services.AddMessageWriter<string, QuotingStringWriter>());
```

`WriteBold`, `WriteError` and their line forms do not go through the writers: they ask the
formatters for the text, escape it and render it with `Markup`.

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

**Mind the exit code.** A failed `Validate` is reported by `Spectre.Console.Cli` itself, before
`DoExecute` runs, and the application exits with `ExitCode.InvalidInput` (2): the handler
`AppBuilder.ConfigureCommandApp` installs maps Spectre's parse, binding and validation failures to
that code in place of Spectre's own `-1`. `ExitCode.InvalidInput` is also what your command returns
when it rejects input itself; see [Exit codes](#11-exit-codes).

## 9. Token expansion in settings

Mark a string setting with `[SupportsTokens]` and the `CommandArgumentsRootProcessor` rewrites its
value before `DoExecute` / `DoExecuteAsync` runs. `{date}` and `{datetime}` are resolved out of the box.

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
Processing File: dataset.csv
Resolved Output Path (with tokens replaced): ./out-2026-08-22/result.dat
Backup enabled: True

Processing file content...
File processed successfully!
Saved result to: ./out-2026-08-22/result.dat
```

Token expansion happens for every command whose base class runs the settings processor —
`AppCommand<TSettings>`, `AsyncAppCommand<TSettings>` and `UseCaseAsyncCommand<...>` all do.

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
| `Error` | 1 | The command ran and failed, or the application failed before it could run. |
| `InvalidInput` | 2 | The input was rejected — by the command, or by `Spectre.Console.Cli` while parsing, binding or validating it. |
| `Cancelled` | 130 | The command stopped because cancellation was requested (128 + SIGINT). |

`InvalidInput` is also the exit code when `Spectre.Console.Cli` could not parse or bind the command
line, or the settings failed `Validate` — an unknown command, an unknown option under strict
parsing, a missing required argument, a value that does not convert. Spectre reports the error and
the application exits with 2 before your command runs; Spectre's own code for these is `-1` (`255`
in a POSIX shell), which `AppBuilder.ConfigureCommandApp` replaces by installing an exception
handler. A `SetExceptionHandler` or `PropagateExceptions()` call in your `ConfigureCommandApp`
delegate replaces that handler. Spectre raises the same exception type for a few failures that are
not input errors — a settings property with no type converter, a command dependency it could not
resolve — so those exit 2 as well; an application that needs to tell them apart installs its own
handler.

Two exit codes do **not** come from this enumeration:

- **Whatever `IExceptionHandler` returns** — an unhandled exception inside a command is routed to
  the handler, and the handler's return value is the exit code. That includes an exception escaping
  a plain `Spectre.Console.Cli` command that does not derive from `AppCommand<TSettings>`.
- **`1`, with help shown** — `Spectre.Console.Cli`'s own early return for a branch invoked without a
  sub-command, or a default command invoked without its required arguments. Numerically the same
  as `ExitCode.Error`, but produced by Spectre, not by this library.

A failure before the host exists — an exception thrown by a `ConfigureServices` delegate while
the host is being built, or a configuration defect Spectre finds while building its command
model — is reported and exits with `ExitCode.Error` (1), as is an
`IExceptionHandler` that throws, or that cannot be constructed when the application resolves it
to handle an exception. A command derived from the base classes constructor-injects the handler,
so a handler that cannot be constructed stops that command being constructed, which Spectre
reports as a `CommandRuntimeException` and the application exits 2. Requested cancellation is
`ExitCode.Cancelled` (130); only a plain `Spectre.Console.Cli` command can change that, see
[Cancellation](#12-cancellation).

The [exit codes summary](https://commandline.github.ploch.dev/#exit-codes) on the documentation site
lists the same codes.

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

A destructive command needs the same discipline in reverse: confirm before acting, and distinguish
"you did not tell me it was safe to proceed" from "you told me to stop".

```csharp
if (!AnsiConsole.Profile.Capabilities.Interactive)
{
    Output.MarkupLineInterpolated($"[yellow]Refusing to delete user {userId} without confirmation. Re-run with --force.[/]");

    return ExitCode.InvalidInput;   // nobody can answer a prompt in a pipeline
}

return AnsiConsole.Confirm($"Delete user {userId}?", defaultValue: false) ? null : ExitCode.Cancelled;
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

The base class treats cancellation as an outcome rather than a fault, but only when it was
requested through the token the command received: an `OperationCanceledException` thrown while
that token has cancellation requested is not passed to `IExceptionHandler`, it becomes
`ExitCode.Cancelled` (130). An `OperationCanceledException` raised while the token is still live
— a `TaskCanceledException` from an `HttpClient` timeout, or an inner token the command created
and cancelled itself — is a fault nobody asked for, so it goes to the exception handler like any
other exception.

The base classes always return `ExitCode.Cancelled` for requested cancellation. A requested
cancellation that reaches `Spectre.Console.Cli` instead — thrown by a command that does not derive
from the base classes, or while the settings were being bound — exits with Spectre's configured
cancellation exit code, which is also 130 unless the `ConfigureCommandApp` delegate calls
`CancellationExitCode(...)`.

### Where the token comes from

`AppBuilder.Create` owns the `CancellationTokenSource`, and `ConfigureCommandApp` hands its token to
Spectre, which passes it to every command. The source is also registered in the container, so a
command that needs to request shutdown itself can resolve a `CancellationTokenSource` and cancel it.

Interrupting the application from the keyboard follows a two-step contract:

| Interrupt | Effect |
|---|---|
| First Ctrl+C | Cancels the token; the command is expected to stop cooperatively. The application prints `Shutting down... press Ctrl+C again to force an exit.` |
| Second Ctrl+C | Takes the default path and terminates the process, so a command that ignores its token cannot hang the application |

A cancellation callback that throws is reported rather than allowed to escape — an unhandled
exception on the `Console.CancelKeyPress` thread would terminate the process, which is the opposite
of the shutdown being requested.

`EnvironmentSettings.PauseBeforeExit` is skipped after cancellation: prompting for Enter on the way
out of a shutdown the user just asked for would turn it into a hang.

### One run per executor

`ConfigureCommandApp` returns an executor that permits one execution attempt. An attempt may build
the host behind it, and that host cannot be built again, so every attempt consumes the executor
whatever its outcome: a second `Run` / `RunAsync` on the same executor throws
`InvalidOperationException`. To run again, call `ConfigureCommandApp` on the builder once more; it
configures a fresh host builder over the same recorded configuration and the same cancellation
source — the host itself is built when the new executor runs. Cancellation is permanent, so a source
that was cancelled stays cancelled, and `ConfigureCommandApp` on the same builder after Ctrl+C throws
`InvalidOperationException` rather than hand out an executor whose every command would start with an
already-cancelled token; to run again after cancellation, start from a new builder
(`AppBuilder.Create`) or pass a fresh `CancellationTokenSource` to the constructor.

## 13. Logging with Serilog

`Ploch.CommandLine.Spectre.Serilog` configures Serilog as the logging provider, with the
Spectre.Console console sink and two rolling files — everything, and warnings and above. The
core package registers it with log files named after the process; call `AddSerilog` to choose
your own name and directory:

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

Both log files are written with `CultureInfo.InvariantCulture`, so numbers and dates in them read the
same on every machine, whatever its locale. If you prefer locale-formatted values in the files, pass
`culture: System.Globalization.CultureInfo.CurrentCulture` to `AddSerilog`. Console output is not
affected.

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
`Execute` / `ExecuteAsync` — that path runs the settings processor and turns exceptions and
cancellation into exit codes, as well as your `DoExecute` body. It does not run validation:
`Spectre.Console.Cli` calls `Validate` before `Execute`, so a direct call skips it. Test `Validate`
(or the `ICommandSettingsValidator<TSettings>` behind it) separately.

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
- **Cancellation.** Pass an already-cancelled token to an implementation that observes it (for
  example one that calls `cancellationToken.ThrowIfCancellationRequested()`) and assert
  `ExitCode.Cancelled`, plus that the exception handler was never called — that proves cancellation
  is not being reported as a failure. Then throw an `OperationCanceledException` with the token
  still live and assert the handler received it — that proves a cancellation nobody asked for is
  not reported as one.

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

See [`samples/SampleApp/README.md`](https://github.com/mrploch/ploch-commandline/blob/main/samples/SampleApp/README.md) for the full command tour and
for the difference between the standalone (NuGet) and in-repository (project reference) build
modes.

If you copy that switching trick into a sample of your own, put the conditional import in
`Directory.Build.targets` rather than `Directory.Build.props`: props is evaluated before the project
body, so `<PackageReference Remove="..." />` would have nothing to remove and the project would end
up with both a package reference and a project reference to every library.
