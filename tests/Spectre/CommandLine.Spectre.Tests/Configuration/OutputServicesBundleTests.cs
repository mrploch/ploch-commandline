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
    public void WriteMessage_should_report_that_a_string_was_handled()
    {
        using var provider = BuildProvider();
        var processor = provider.GetRequiredService<IMessageFormatterProcessor>();

        processor.WriteMessage("a string").Should().BeTrue("a writer is registered for string");
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

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddServicesBundle(new OutputServicesBundle());

        return services.BuildServiceProvider();
    }
}
