using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre.Configuration;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Ploch.Common.DependencyInjection;
using Spectre.Console;

namespace Ploch.CommandLine.Spectre.Tests.Configuration;

/// <summary>
///     Integration cover for the output pipeline as it is actually wired in DI.
///     This is the test that would have caught both consumer-visible output defects: a formatter registered for
///     every <see cref="IConvertible" /> that threw <see cref="NotImplementedException" />, and a write path
///     that emitted handled messages twice.
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class OutputServicesBundleTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly RecordingConsole _console = new();

    public OutputServicesBundleTests()
    {
        // The bundle captures AnsiConsole.Console when it registers, so the swap has to happen before the provider is built.
        AnsiConsole.Console = _console.Console;
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _console.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Bundle_should_resolve_the_message_formatter_processor()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IMessageFormatterProcessor>().Should().NotBeNull();
    }

    [Fact]
    public void Bundle_should_resolve_the_output()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IOutput>().Should().BeOfType<AnsiConsoleMarkupOutput>();
    }

    [Theory]
    [InlineData(42)]
    [InlineData(true)]
    [InlineData(3.5)]
    public void GetMessageText_should_not_throw_for_convertible_values(object message)
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        var act = () => processor.GetMessageText(message);

        act.Should().NotThrow("every IConvertible resolves to ConvertibleMessageFormatter through DI");
    }

    [Fact]
    public void GetMessageText_should_format_an_exception_through_the_registered_formatter()
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        var result = processor.GetMessageText(new InvalidOperationException("failure text"));

        result.Should().Contain("failure text");
    }

    [Fact]
    public void WriteMessage_should_report_the_writer_that_handled_a_string()
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        processor.WriteMessage("a string").Should().NotBeNull("a writer is registered for string");
    }

    [Fact]
    public void Write_should_render_an_exception_through_the_registered_exception_writer()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        var act = () => output.Write(new InvalidOperationException("probe failure"));

        act.Should().NotThrow("the writer registered for Exception must be handed the exception, not its formatted text");
        _console.Output.Should().Contain("probe failure");
    }

    [Fact]
    public void Write_should_render_one_line_per_item_for_a_collection()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        output.Write<IEnumerable<string>>(["alpha", "beta"]);

        _console.Output.Should()
                .Be($"alpha{Environment.NewLine}beta{Environment.NewLine}",
                    "the writer registered for IEnumerable receives the collection, so it enumerates the items rather than the characters of a formatted string");
    }

    /// <summary>
    ///     The string writer was registered but never selected: the output wrote strings itself before consulting the
    ///     processor. These tests pin the writer path as wired in DI, where the enumerable writer also accepts a string.
    /// </summary>
    [Fact]
    public void Write_should_render_a_string_verbatim_through_the_registered_string_writer()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        output.Write("[not markup] abc");

        _console.Output.Should().Be("[not markup] abc", "the string writer wins over the enumerable writer and writes the text as-is");
    }

    [Fact]
    public void WriteLine_should_add_exactly_one_terminator_after_a_string()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        output.WriteLine("abc");

        _console.Output.Should().Be("abc" + Environment.NewLine, "one character per line would mean the enumerable writer took the string");
    }

    [Fact]
    public void Write_should_use_a_string_writer_registered_after_the_bundle()
    {
        using var provider = BuildProvider(services => services.AddMessageWriter<string, UpperCasingStringWriter>());
        var output = provider.GetRequiredService<IOutput>();

        output.Write("abc");

        _console.Output.Should().Be("ABC", "a consumer's writer for a type the bundle covers replaces the built-in one");
    }

    [Fact]
    public void WriteLine_should_use_a_string_writer_registered_after_the_bundle_and_terminate_the_line()
    {
        using var provider = BuildProvider(services => services.AddMessageWriter<string, UpperCasingStringWriter>());
        var output = provider.GetRequiredService<IOutput>();

        output.WriteLine("abc");

        _console.Output.Should().Be("ABC" + Environment.NewLine);
    }

    [Fact]
    public void Write_should_use_an_interpolated_string_writer_registered_after_the_bundle()
    {
        using var provider = BuildProvider(services => services.AddMessageWriter<FormattableString, FormatOnlyWriter>());
        var output = provider.GetRequiredService<IOutput>();

        var value = "x";

        output.Write<FormattableString>($"[bold]{value}[/]");

        _console.Output.Should().Be("[bold]{0}[/]", "the consumer's writer replaced the built-in markup rendering");
    }

    [Fact]
    public void Write_should_render_an_interpolated_string_as_markup_with_its_holes_escaped()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        var literal = "[literal]";

        output.Write<FormattableString>($"[bold]{literal}[/]");

        _console.Output.Should().Be("[literal]", "the template's tag is parsed and the hole is escaped");
    }

    [Fact]
    public void Write_should_keep_the_format_specifier_of_an_interpolated_hole()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();

        output.Write<FormattableString>($"total: {1234.5:N2}", CultureInfo.InvariantCulture);

        _console.Output.Should().Be("total: 1,234.50", "the registered convertible formatter must not pre-render the hole and lose N2");
    }

    [Fact]
    public void Write_should_render_an_exception_in_an_interpolated_hole_with_its_own_ToString()
    {
        using var provider = BuildProvider();
        var output = provider.GetRequiredService<IOutput>();
        var exception = new InvalidOperationException("probe failure");

        output.Write<FormattableString>($"failed: {exception}");

        _console.Output.Should().Be($"failed: {exception}", "an interpolation hole renders as string.Format would, not through the exception formatter");
    }

    [Fact]
    public void Bundle_should_register_every_message_formatter_exactly_once()
    {
        var services = new ServiceCollection();
        services.AddServicesBundle(new OutputServicesBundle());

        var formatterDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IMessageFormatter)).ToList();

        formatterDescriptors.Select(descriptor => descriptor.ImplementationType)
                            .Should()
                            .OnlyHaveUniqueItems("a formatter registered twice would be instantiated twice");
    }

    /// <summary>
    ///     The processor picks the first formatter whose <c>CanHandle</c> accepts the message, and <c>CanHandle</c> is
    ///     an <c>IsInstanceOfType</c> check. Registering the general exception formatter ahead of the Win32 one made
    ///     the Win32 formatter unreachable, so its native error code never appeared.
    /// </summary>
    [Fact]
    public void GetMessageText_should_prefer_the_Win32_formatter_over_the_general_exception_formatter()
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        var text = processor.GetMessageText(new Win32Exception(5));

        text.Should().Contain("Error Code", "the more specific Win32 formatter reports the native error code");
        text.Should().Contain("5");
    }

    [Fact]
    public void GetMessageText_should_still_use_the_general_formatter_for_a_plain_exception()
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        var text = processor.GetMessageText(new InvalidOperationException("boom"));

        text.Should().Contain("boom");
        text.Should().NotContain("Error Code", "a plain exception has no native error code to report");
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configureAfterBundle = null)
    {
        var services = new ServiceCollection();
        services.AddServicesBundle(new OutputServicesBundle());

        // Mirrors AppBuilder, where the bundles register before any caller ConfigureServices delegate.
        configureAfterBundle?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>A consumer's replacement for the built-in interpolated-string writer that prints only the template.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local - the container instantiates it through AddMessageWriter.
    private sealed class FormatOnlyWriter(IAnsiConsole console) : TypeMessageWriter<FormattableString>
    {
        public override void Write(FormattableString? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
            console.Write(message?.Format ?? string.Empty);
    }

    /// <summary>A consumer's replacement for the built-in string writer, visibly different from a verbatim write.</summary>
    // ReSharper disable once ClassNeverInstantiated.Local - the container instantiates it through AddMessageWriter.
    private sealed class UpperCasingStringWriter(IAnsiConsole console) : TypeMessageWriter<string>
    {
        public override void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null, IFormatProvider? formatProvider = null) =>
            console.Write(message?.ToUpperInvariant() ?? string.Empty);
    }
}
