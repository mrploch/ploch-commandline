using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.FluentValidation.Tests;

public class TestCommandSettings : CommandSettings
{
    public string? NotEmptyStringProperty { get; set; }

    public int PositiveIntProperty { get; set; }
}
