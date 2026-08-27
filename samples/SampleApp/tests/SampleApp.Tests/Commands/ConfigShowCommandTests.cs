using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.SampleApp.Tests.Commands;

/// <summary>
///     Cover for the disclosure policy on <see cref="ConfigShowCommand" />. Its two siblings render strings, so a
///     mocked <see cref="IOutput.MarkupLineInterpolated" /> is enough to inspect them; this one builds a
///     <see cref="Tree" /> and hands it over as an <see cref="IRenderable" />, so the renderable is captured and
///     rendered here to assert on what a user would actually see.
/// </summary>
public class ConfigShowCommandTests
{
    private readonly Mock<ICommandSettingsValidator<ConfigShowCommandSettings>> _validatorMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();

    /// <summary>
    ///     The section allow-list keeps whole trees out, but it does not make the leaves inside them safe: the
    ///     environment provider can populate any key beneath an allowed section, and the recursion renders every
    ///     leaf it reaches.
    /// </summary>
    [Fact]
    public void Execute_should_redact_a_nested_value_whose_path_names_a_secret()
    {
        var (command, rendered) = CreateCommand(new Dictionary<string, string?>
                                                {
                                                    ["Serilog:WriteTo:0:Args:apiKey"] = "super-secret-value",
                                                    ["Serilog:MinimumLevel"] = "Information",
                                                });

        var result = command.Execute(CreateContext(), new ConfigShowCommandSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        rendered().Should().NotContain("super-secret-value", "a sensitive leaf inside an allowed section must still be redacted");
        rendered().Should().Contain("redacted");
    }

    [Fact]
    public void Execute_should_render_an_ordinary_nested_value()
    {
        var (command, rendered) = CreateCommand(new Dictionary<string, string?> { ["Serilog:MinimumLevel"] = "Information" });

        command.Execute(CreateContext(), new ConfigShowCommandSettings(), CancellationToken.None);

        rendered().Should().Contain("Information", "the redaction must be driven by the key, not applied to every leaf");
    }

    /// <summary>
    ///     The host adds an environment-variable provider, so enumerating the configuration root would print every
    ///     environment variable of the process. Only the sections this application owns may be rendered.
    /// </summary>
    [Fact]
    public void Execute_should_not_render_sections_the_application_does_not_own()
    {
        var (command, rendered) = CreateCommand(new Dictionary<string, string?>
                                                {
                                                    ["Serilog:MinimumLevel"] = "Information",
                                                    ["AWS_SECRET_ACCESS_KEY"] = "super-secret-value",
                                                });

        command.Execute(CreateContext(), new ConfigShowCommandSettings(), CancellationToken.None);

        rendered().Should().NotContain("super-secret-value", "a key outside the allow-list must never be enumerated");
        rendered().Should().NotContain("AWS_SECRET_ACCESS_KEY");
    }

    private static CommandContext CreateContext() => new([], Mock.Of<IRemainingArguments>(), "show", null);

    private (ConfigShowCommand Command, Func<string> Rendered) CreateCommand(Dictionary<string, string?> values)
    {
        var renderables = new List<IRenderable>();
        var outputMock = new Mock<IOutput>();

        // The generic Write<TMessage> overload, not Write(IRenderable): for a Tree argument the generic is an
        // exact match while the IRenderable overload needs a conversion, so overload resolution picks the generic
        // one. Mocking IRenderable here captures nothing, and every assertion then passes against an empty string.
        outputMock.Setup(output => output.Write(It.IsAny<Tree>(), It.IsAny<IFormatProvider?>()))
                  .Callback<Tree, IFormatProvider?>((tree, _) => renderables.Add(tree))
                  .Returns(() => outputMock.Object);
        outputMock.Setup(output => output.MarkupLineInterpolated(It.IsAny<FormattableString>())).Returns(() => outputMock.Object);
        outputMock.Setup(output => output.WriteLine()).Returns(() => outputMock.Object);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var command = new ConfigShowCommand(_validatorMock.Object, _exceptionHandlerMock.Object, outputMock.Object, configuration);

        return (command, () => Render(renderables));
    }

    /// <summary>Renders the captured tree to plain text, so assertions read what a user would see.</summary>
    private static string Render(IEnumerable<IRenderable> renderables)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
                                         {
                                             Ansi = AnsiSupport.No,
                                             ColorSystem = ColorSystemSupport.NoColors,
                                             Out = new AnsiConsoleOutput(writer),
                                         });

        foreach (var renderable in renderables)
        {
            console.Write(renderable);
        }

        return writer.ToString();
    }
}
