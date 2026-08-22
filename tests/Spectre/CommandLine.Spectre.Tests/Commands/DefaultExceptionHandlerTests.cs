using System.ComponentModel;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the default exception handler. A <see cref="Win32Exception" /> is deliberately routed around the
///     markup pipeline: its text contains '[' sequences that Spectre would parse as markup and throw on, inside
///     the handler that is supposed to be reporting the original failure.
/// </summary>
public class DefaultExceptionHandlerTests
{
    [Fact]
    public void HandleException_should_return_the_error_exit_code()
    {
        using var console = new RecordingConsole();
        var handler = new DefaultExceptionHandler(console.Console, new AnsiConsoleMarkupOutput(console.Console, new MessageFormatterProcessor([], [])));

        handler.HandleException(new InvalidOperationException("boom")).Should().Be((int)ExitCode.Error);
    }

    [Fact]
    public void HandleException_should_render_an_ordinary_exception_through_the_output()
    {
        using var console = new RecordingConsole();
        var output = new RecordingOutput();
        var handler = new DefaultExceptionHandler(console.Console, output);

        var exception = new InvalidOperationException("ordinary failure");
        handler.HandleException(exception);

        output.Exceptions.Should().ContainSingle().Which.Should().BeSameAs(exception);
        console.Output.Should().BeEmpty("an ordinary exception goes through the markup-aware output, not straight to the console");
    }

    [Fact]
    public void HandleException_should_bypass_the_markup_output_for_a_Win32_exception()
    {
        using var console = new RecordingConsole();
        var output = new RecordingOutput();
        var handler = new DefaultExceptionHandler(console.Console, output);

        handler.HandleException(new Win32Exception(5));

        output.Exceptions.Should().BeEmpty("a Win32 exception must not reach the markup parser");
        console.Output.Should().Contain(nameof(Win32Exception));
    }

    [Fact]
    public void HandleException_should_bypass_the_markup_output_when_a_Win32_exception_is_the_inner_exception()
    {
        using var console = new RecordingConsole();
        var output = new RecordingOutput();
        var handler = new DefaultExceptionHandler(console.Console, output);

        handler.HandleException(new InvalidOperationException("wrapper", new Win32Exception(5)));

        output.Exceptions.Should().BeEmpty("the unsafe text is still present when the Win32 exception is nested");
        console.Output.Should().Contain(nameof(Win32Exception));
    }

    [Fact]
    public void HandleException_should_write_a_Win32_exception_without_markup_parsing_failing()
    {
        using var console = new RecordingConsole();
        var handler = new DefaultExceptionHandler(console.Console, new AnsiConsoleMarkupOutput(console.Console, new MessageFormatterProcessor([], [])));

        var act = () => handler.HandleException(new Win32Exception(5, "text with [square] brackets"));

        act.Should().NotThrow("the handler must not throw on the content it is reporting");
        console.Output.Should().Contain("[square]");
    }

    /// <summary>Records the exceptions passed to the output so the routing decision can be asserted.</summary>
    private sealed class RecordingOutput : NoOpOutput
    {
        public List<Exception?> Exceptions { get; } = [];

        protected override void OnException(Exception? exception) => Exceptions.Add(exception);
    }
}
