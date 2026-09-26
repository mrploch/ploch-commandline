# Ploch.CommandLine.UseCases

Use-case abstractions for command-line applications built with
[Ploch.CommandLine.Spectre](https://www.nuget.org/packages/Ploch.CommandLine.Spectre). Keep the work in a
use case: `IUseCase<TRequest, TResponse>` is the general abstraction, and
`IResultUseCase<TRequest, TResponse>` is the variant whose response is an
[Ardalis.Result](https://github.com/ardalis/Result).

`UseCaseAsyncCommand` runs an **`IResultUseCase`** from a command: it turns the command settings into a
request, runs the use case, reports a failed result's errors and validation errors, and maps the result
to an exit code. On success it prints a completion message; override `ProcessSuccessResponse` to render
the response value itself.

## Installation

```bash
dotnet add package Ploch.CommandLine.UseCases
```

## Example

```csharp
using Ardalis.Result;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.UseCases;
using Spectre.Console.Cli;

public record GreetRequest(string Name);

public class GreetUseCase : IResultUseCase<GreetRequest, string>
{
    public Task<Result<string>> ExecuteAsync(GreetRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success($"Hello, {request.Name}!"));
}

public class GreetSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    public string Name { get; set; } = "world";
}

public class GreetCommand(IOutput output,
                          GreetUseCase useCase,
                          CommandArgumentsRootProcessor settingsProcessor,
                          ICommandSettingsValidator<GreetSettings> validator,
                          IExceptionHandler exceptionHandler)
    : UseCaseAsyncCommand<GreetSettings, GreetUseCase, GreetRequest, string>(output, useCase, settingsProcessor, validator, exceptionHandler)
{
    protected override GreetRequest CreateRequest(GreetSettings settings) => new(settings.Name);
}
```

Register the use case in the container and the command with `ConfigureCommandApp`, exactly as for any
other command.

## Documentation

- [Getting Started guide](https://github.com/mrploch/ploch-commandline/blob/main/docs/GETTING_STARTED.md)
- [Documentation site](https://commandline.github.ploch.dev/)
