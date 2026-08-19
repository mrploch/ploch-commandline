using FluentAssertions;
using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

public class TypeMessageHandlerTests
{
    [Fact]
    public void CanHandle_should_return_false_when_message_is_not_of_type_TMessage()
    {
        var handler = new TestMessageHandler();
        var message = new AnotherMessage();

        var result = handler.CanHandle(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_should_return_false_when_message_is_null()
    {
        var handler = new TestMessageHandler();

        var result = handler.CanHandle(null);

        result.Should().BeFalse();
    }

    [Fact]
#pragma warning disable CA1707
    public void CanHandle_should_return_true_when_message_is_of_type_TMessage()
#pragma warning restore CA1707
    {
        var handler = new TestMessageHandler();
        var message = new TestMessage();

        var result = handler.CanHandle(message);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_should_return_true_when_message_type_is_derived_from_TestMessage()
    {
        var handler = new TestMessageHandler();
        var message = new DerivedTestMessage();

        var result = handler.CanHandle(message);

        result.Should().BeTrue();
    }

    [Fact]
    public void MessageType_should_return_correct_type()
    {
        var handler = new TestMessageHandler();

        var messageType = handler.MessageType;

        messageType.Should().Be(typeof(TestMessage));
    }

    private class TestMessage
    { }

    private class AnotherMessage
    { }

    private class DerivedTestMessage : TestMessage
    { }

    private class TestMessageHandler : TypeMessageHandler<TestMessage>
    { }
}
