namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Describes a replaceable token that can be substituted into command argument values.
/// </summary>
/// <param name="TokenName">The name of the token, without delimiters.</param>
/// <param name="ValueProvider">Supplies the token's value for general substitution.</param>
/// <param name="PathSafeValueProvider">Supplies the token's value sanitised for use within a file system path.</param>
public record TokenInfo(string TokenName, Func<string> ValueProvider, Func<string> PathSafeValueProvider);
