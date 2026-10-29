# API reference

Generated from the XML documentation of the four packages. Every public type and member is listed;
this page says which namespace to open for what.

## Ploch.CommandLine.Spectre

The core package, in five namespaces:

- [`Ploch.CommandLine.Spectre`](xref:Ploch.CommandLine.Spectre) — the entry point.
  [`AppBuilder`](xref:Ploch.CommandLine.Spectre.AppBuilder) wraps the host and `Spectre.Console.Cli`;
  `ConfigureCommandApp` returns the [`ICommandAppExecutor`](xref:Ploch.CommandLine.Spectre.ICommandAppExecutor)
  that runs the application. `ConsoleAppInfo` and `EnvironmentSettings` describe the application and
  the environment it runs in.
- [`Ploch.CommandLine.Spectre.Commands`](xref:Ploch.CommandLine.Spectre.Commands) — what a command
  derives from and returns: [`AppCommand<TSettings>`](xref:Ploch.CommandLine.Spectre.Commands.AppCommand%601),
  [`AsyncAppCommand<TSettings>`](xref:Ploch.CommandLine.Spectre.Commands.AsyncAppCommand%601),
  [`ExitCode`](xref:Ploch.CommandLine.Spectre.Commands.ExitCode),
  [`IExceptionHandler`](xref:Ploch.CommandLine.Spectre.Commands.IExceptionHandler) and the
  settings-processing pipeline (`CommandArgumentsRootProcessor`, `TokensArgumentsProcessor` and
  `[SupportsTokens]`).
- [`Ploch.CommandLine.Spectre.Output`](xref:Ploch.CommandLine.Spectre.Output) — the output pipeline:
  [`IOutput`](xref:Ploch.CommandLine.Spectre.Output.IOutput), the message formatters and writers behind
  it, and the `AddMessageWriter` / `AddMessageFormatter` registration extensions.
- [`Ploch.CommandLine.Spectre.Configuration`](xref:Ploch.CommandLine.Spectre.Configuration) — the
  services bundles the builder registers by default.
- [`Ploch.CommandLine.Spectre.DependencyInjection`](xref:Ploch.CommandLine.Spectre.DependencyInjection) —
  the Spectre type registrar and resolver backed by `Microsoft.Extensions.DependencyInjection`, and
  the lifetime rules they apply.

## Ploch.CommandLine.Spectre.Serilog

[`Ploch.CommandLine.Spectre.Serilog`](xref:Ploch.CommandLine.Spectre.Serilog) — opt-in Serilog logging:
[`UseSerilog`](xref:Ploch.CommandLine.Spectre.Serilog.AppBuilderSerilogExtensions) on the builder,
`AddSerilog` on a service collection, the `SerilogConfigurationBundle` behind both, and
`ConfigureSerilog` for a `LoggerConfiguration` of your own.

## Ploch.CommandLine.Spectre.FluentValidation

[`Ploch.CommandLine.Spectre.FluentValidation`](xref:Ploch.CommandLine.Spectre.FluentValidation) —
`AddCommandLineSettingsFluentValidation`, which registers
[`FluentCommandSettingsValidator<TSettings>`](xref:Ploch.CommandLine.Spectre.FluentValidation.FluentCommandSettingsValidator%601)
as the validator the command base classes call, and the services bundle it is built on.

## Ploch.CommandLine.UseCases

[`Ploch.CommandLine.UseCases`](xref:Ploch.CommandLine.UseCases) —
[`IUseCase<TRequest, TResponse>`](xref:Ploch.CommandLine.UseCases.IUseCase%602),
[`IResultUseCase<TRequest, TResponse>`](xref:Ploch.CommandLine.UseCases.IResultUseCase%602) and
[`UseCaseAsyncCommand<...>`](xref:Ploch.CommandLine.UseCases.UseCaseAsyncCommand%604), the command base
class that runs a result use case and maps its `Ardalis.Result` to output and an exit code.
