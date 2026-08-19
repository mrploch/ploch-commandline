using Ploch.CommandLine.Spectre.Commands;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for the property-collection base used by settings processors: attribute filtering, and the reset
///     between invocations that stops a reused instance throwing on a repeated property name.
/// </summary>
public class CommandSettingsPropertyTypeProcessorTests
{
    [Fact]
    public void ProcessArguments_should_collect_every_property_of_the_supported_type_when_no_attribute_is_required()
    {
        var processor = new RecordingProcessor();

        processor.ProcessArguments(new SampleSettings());

        processor.CollectedNames.Should().Contain([nameof(SampleSettings.Tagged), nameof(SampleSettings.Untagged)]);
    }

    [Fact]
    public void ProcessArguments_should_skip_properties_missing_a_required_attribute()
    {
        var processor = new RecordingProcessor { Required = [typeof(SupportsTokensAttribute)] };

        processor.ProcessArguments(new SampleSettings());

        processor.CollectedNames.Should().Contain(nameof(SampleSettings.Tagged));
        processor.CollectedNames.Should().NotContain(nameof(SampleSettings.Untagged));
    }

    [Fact]
    public void ProcessArguments_should_reset_collected_properties_between_invocations()
    {
        var processor = new RecordingProcessor();

        processor.ProcessArguments(new SampleSettings());
        var act = () => processor.ProcessArguments(new SampleSettings());

        act.Should().NotThrow("a repeated property name would otherwise collide with the previous run");
    }

    [Fact]
    public void ProcessArguments_should_reflect_only_the_most_recent_settings_instance()
    {
        var processor = new RecordingProcessor();

        processor.ProcessArguments(new SampleSettings());
        processor.ProcessArguments(new SampleSettings());

        processor.CollectedNames.Count(name => name == nameof(SampleSettings.Tagged))
                 .Should()
                 .Be(1, "properties from the previous invocation must not linger");
    }

    [Fact]
    public void SupportedPropertyType_should_report_the_generic_argument()
    {
        new RecordingProcessor().SupportedPropertyType.Should().Be<string>();
    }

    private sealed class SampleSettings : CommandSettings
    {
        [SupportsTokens]
        public string? Tagged { get; set; }

        public string? Untagged { get; set; } = "untagged value";
    }

    private sealed class RecordingProcessor : CommandSettingsPropertyTypeProcessor<string>
    {
        public List<string> CollectedNames { get; } = [];

        public override Type[] RequiredAttributes => Required;

        public Type[] Required { get; init; } = [];

        protected override void DoProcessArguments()
        {
            CollectedNames.Clear();
            CollectedNames.AddRange(Properties.Keys);
        }
    }
}
