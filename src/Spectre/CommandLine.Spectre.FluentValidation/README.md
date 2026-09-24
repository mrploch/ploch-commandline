# Ploch.CommandLine.Spectre.FluentValidation

[FluentValidation](https://docs.fluentvalidation.net/) integration for
[Ploch.CommandLine.Spectre](https://www.nuget.org/packages/Ploch.CommandLine.Spectre): command settings are
validated by your FluentValidation validators before the command runs, and a failed validation is
reported to the user instead of executing the command.

## Installation

```bash
dotnet add package Ploch.CommandLine.Spectre.FluentValidation
```

## Example

```csharp
using FluentValidation;
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.FluentValidation;
using Spectre.Console.Cli;

using var appBuilder = AppBuilder.Create(args)
                                 .ConfigureServices((_, services) =>
                                     services.AddCommandLineSettingsFluentValidation(
                                         assemblies => assemblies.AddAssembly(typeof(GreetSettingsValidator).Assembly)));

// Register your commands with appBuilder.ConfigureCommandApp(...) and run them as usual.

public class GreetSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    public string Name { get; set; } = string.Empty;
}

public class GreetSettingsValidator : AbstractValidator<GreetSettings>
{
    public GreetSettingsValidator() => RuleFor(settings => settings.Name).NotEmpty();
}
```

Every `AbstractValidator<TSettings>` in the scanned assemblies is picked up; commands derived from
`AppCommand<TSettings>` or `AsyncAppCommand<TSettings>` validate their settings automatically.

## Documentation

- [Getting Started guide](https://github.com/mrploch/ploch-commandline/blob/main/docs/GETTING_STARTED.md)
- [Documentation site](https://commandline.github.ploch.dev/)
