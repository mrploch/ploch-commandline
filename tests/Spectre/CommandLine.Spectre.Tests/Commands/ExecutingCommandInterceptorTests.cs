using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for <see cref="ExecutingCommandInterceptor" />, which announces the command about to run on the console
///     it is given — the standard error console when <see cref="AppBuilder" /> registers it — naming the command as
///     the user invoked it rather than by its settings type (PLO-783).
/// </summary>
public sealed class ExecutingCommandInterceptorTests : IDisposable
{
    private readonly RecordingConsole _console = new();

    public void Dispose() => _console.Dispose();

    [Fact]
    public void Intercept_should_announce_the_command_by_its_name()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);

        interceptor.Intercept(Context("add"), new StubSettings());

        _console.Output.Should().Be("Executing command add" + Environment.NewLine);
    }

    [Fact]
    public void Intercept_should_not_name_the_settings_type()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);

        interceptor.Intercept(Context("add"), new StubSettings());

        _console.Output.Should().NotContain(nameof(StubSettings), "the line names the command the user typed, not a type in the code");
    }

    /// <summary>
    ///     The command name is the consumer's and goes through an interpolated hole, which Spectre escapes, so a name
    ///     with brackets is rendered as text rather than parsed as a style tag.
    /// </summary>
    [Fact]
    public void Intercept_should_not_parse_the_command_name_as_markup()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);

        var act = () => interceptor.Intercept(Context("add[all]"), new StubSettings());

        act.Should().NotThrow();
        _console.Output.Should().Contain("Executing command add[all]");
    }

    /// <summary>
    ///     Spectre names a default command with an internal placeholder; the user typed no name, so none is shown.
    /// </summary>
    [Fact]
    public void Intercept_should_announce_a_default_command_without_exposing_spectres_placeholder_name()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);

        interceptor.Intercept(Context(ExecutingCommandInterceptor.SpectreDefaultCommandName), new StubSettings());

        _console.Output.Should().Be("Executing the default command" + Environment.NewLine);
    }

    [Fact]
    public void Intercept_should_throw_when_the_context_is_null()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);

        var act = () => interceptor.Intercept(null!, new StubSettings());

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void InterceptResult_should_leave_the_exit_code_unchanged()
    {
        var interceptor = new ExecutingCommandInterceptor(_console.Console);
        var result = 42;

        interceptor.InterceptResult(Context("add"), new StubSettings(), ref result);

        result.Should().Be(42, "the exit code is the command's to decide");
        _console.Output.Should().BeEmpty("the announcement is made before the command runs, not after");
    }

    [Fact]
    public void Constructor_should_throw_when_the_console_is_null()
    {
        var act = () => new ExecutingCommandInterceptor(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("errorConsole");
    }

    private static CommandContext Context(string name) => new([], Mock.Of<IRemainingArguments>(), name, null);

    // ReSharper disable once RedundantTypeDeclarationBody - StyleCop rejects the bodiless form (SA1106).
    private sealed class StubSettings : CommandSettings
    {
    }
}
