namespace Ploch.CommandLine.Spectre.Commands;

public record TokenInfo(string TokenName, Func<string> ValueProvider, Func<string> PathSafeValueProvider);
