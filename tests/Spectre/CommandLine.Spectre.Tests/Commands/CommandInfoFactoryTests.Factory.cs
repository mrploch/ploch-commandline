using Ploch.CommandLine.Spectre.Commands;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

/// <summary>
///     Cover for <see cref="CommandInfoFactory" />. The attribute-carrying path previously threw
///     <see cref="NotImplementedException" /> — which is the entire purpose of the factory — and no test
///     exercised it.
/// </summary>
public class CommandInfoFactoryFactoryTests
{
    [Fact]
    public void CreateFromType_should_map_every_attribute_value_onto_the_command_info()
    {
        var result = CommandInfoFactory.CreateFromType(typeof(FullyAttributedCommand));

        result.Name.Should().Be("full");
        result.Alias.Should().Be("f");
        result.Description.Should().Be("A fully attributed command.");
        result.Examples.Should().BeEquivalentTo("full --one", "full --two");
    }

    [Fact]
    public void CreateFromType_should_fall_back_to_the_type_name_when_the_attribute_is_absent()
    {
        var result = CommandInfoFactory.CreateFromType(typeof(UnattributedCommand));

        result.Name.Should().Be(nameof(UnattributedCommand));
        result.Alias.Should().BeNull();
        result.Description.Should().BeNull();
        result.Examples.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromType_should_carry_the_hidden_flag()
    {
        var result = CommandInfoFactory.CreateFromType(typeof(HiddenCommand));

        result.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void CreateFromType_should_default_to_visible_when_the_attribute_does_not_hide_the_command()
    {
        var result = CommandInfoFactory.CreateFromType(typeof(FullyAttributedCommand));

        result.IsHidden.Should().BeFalse();
    }

    [Command("full", "f", "A fully attributed command.", "full --one", "full --two")]
    private sealed class FullyAttributedCommand
    {
    }

    [Command("hidden", IsHidden = true)]
    private sealed class HiddenCommand
    {
    }

    private sealed class UnattributedCommand
    {
    }
}
