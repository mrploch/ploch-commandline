using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

public class TestCommandSettings : CommandSettings
{
    [CommandArgument(0, "[arg]")]
    public string? Arg { get; set; }
}

[Command(nameof(TestCommand), "tc", "Test command for unit testing purposes.", "arg1 some-value")]
public class TestCommand : Command<TestCommandSettings>
{
    public bool Executed { get; private set; }

    public bool Validated { get; private set; }

    public override int Execute(CommandContext context, TestCommandSettings settings, CancellationToken cancellationToken)
    {
        Executed = true;

        return (int)ExitCode.Success;
    }

    public override ValidationResult Validate(CommandContext context, TestCommandSettings settings)
    {
        Validated = true;

        return ValidationResult.Success();
    }
}

[Command(nameof(TestCommand), "tc", "Test command for unit testing purposes.", "arg1 some-value")]
public class InheritedTestCommand : TestCommand
{ }
