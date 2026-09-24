# Wiring up services, configuration, logging, and validation

The [home page](../index.md) covers defining a command and running it. This
article covers the four integration points that turn a single command into a
real application: dependency injection, configuration, logging, and validation.

## Dependency injection

`AppBuilder` builds on `Microsoft.Extensions.Hosting`, so commands are resolved
from the same container as everything else. Register your services with
`ConfigureServices` and take them as constructor parameters:

```csharp
using var appBuilder = AppBuilder.Create(args)
                                 .ConfigureServices(services =>
                                 {
                                     services.AddSingleton<IUserService, UserService>();
                                     services.AddTransient<CreateProjectUseCase>();
                                 });

var executor = appBuilder.ConfigureCommandApp(config => config.AddCommand<UserAddCommand>("add"));
```

If you already package registrations as a `ServicesBundle` from
`Ploch.Common.DependencyInjection`, register the bundle instead:

```csharp
.AddServicesBundle<MyFeatureBundle>()
```

## Configuration

`appsettings.json`, `appsettings.{Environment}.json`, user secrets (in the
`Development` environment only), environment variables and the command-line
arguments are already loaded, in that ascending order of precedence, exactly as
they are in a web host. `ConfigureAppConfiguration` exposes the standard
`IConfigurationBuilder` for anything you want to add on top of them:

```csharp
.ConfigureAppConfiguration(configuration => configuration.AddJsonFile("machine-overrides.json", optional: true))
```

Sources are ordered and the last one added wins, so a source added here overrides
every default — the command line included. That is why you should **not** add
`appsettings.json` a second time to make it load from the deployment directory
rather than the current working directory: it would rank the file above the
command-line arguments it is meant to yield to. Move the content root instead, which
anchors the host's own `appsettings.json` and `appsettings.{Environment}.json`
lookups without touching the precedence order:

```csharp
.ConfigureHost(host => host.UseContentRoot(AppContext.BaseDirectory))
```

Bind the result wherever you need it — `IConfiguration` is available from the
container, as are options types registered with `services.Configure<T>(...)`.

## Logging with Serilog

Add the `Ploch.CommandLine.Spectre.Serilog` package and register its bundle. It
configures Serilog against the application's configuration and routes framework
logging through it:

```csharp
.AddServicesBundle<SerilogConfigurationBundle>()
```

Commands then take `ILogger<T>` as a constructor parameter in the usual way.

> [!NOTE]
> Keep log output and user-facing output separate. Write anything the user is
> meant to read through `IOutput`, and use `ILogger<T>` for diagnostics. Mixing
> the two makes a CLI hard to pipe and hard to script against.

## Validating settings with FluentValidation

Add the `Ploch.CommandLine.Spectre.FluentValidation` package and register your
validators by assembly scan:

```csharp
.ConfigureServices(services =>
    services.AddCommandLineSettingsFluentValidation(builder =>
        builder.AddAssembly(typeof(Program).Assembly)))
```

Then write a validator per settings type:

```csharp
public class UserAddSettingsValidator : AbstractValidator<UserAddSettings>
{
    public UserAddSettingsValidator()
    {
        RuleFor(settings => settings.Name).NotEmpty();
        RuleFor(settings => settings.Email).NotEmpty().EmailAddress();
    }
}
```

Validation runs before `DoExecute`/`DoExecuteAsync`. A failure short-circuits
the command and returns `ExitCode.InvalidInput` (2) without your code running.

## Composing sub-commands

Spectre's configurator supports branches, so a multi-level CLI is a matter of
nesting:

```csharp
.ConfigureCommandApp(config =>
{
    config.SetApplicationName("sample");

    config.AddBranch("user", user =>
    {
        user.SetDescription("Manage user accounts.");
        user.AddCommand<UserAddCommand>("add").WithExample("user", "add", "Alice");
        user.AddCommand<UserListCommand>("list");
    });
});
```

`WithDescription` and `WithExample` feed the generated `--help` output, so
filling them in is what makes the CLI self-documenting.

## Next steps

- Browse the [API documentation](../api/index.md) for the full public surface.
- Read the [sample application](https://github.com/mrploch/ploch-commandline/tree/main/samples), which exercises every feature described here.
