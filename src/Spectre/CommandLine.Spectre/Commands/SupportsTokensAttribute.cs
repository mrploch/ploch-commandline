namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Marks a command settings property as supporting token substitution.
/// </summary>
/// <param name="pathSafe">
///     <see langword="true" /> to substitute path-safe token values, suitable for use within a file system path;
///     otherwise <see langword="false" />. Defaults to <see langword="true" />.
/// </param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SupportsTokensAttribute(bool pathSafe = true) : Attribute
{
    /// <summary>
    ///     Gets a value indicating whether token values are sanitised for use within a file system path.
    /// </summary>
    public bool PathSafe => pathSafe;
}
