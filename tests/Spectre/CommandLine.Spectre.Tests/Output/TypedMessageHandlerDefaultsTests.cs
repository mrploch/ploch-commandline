using Ploch.CommandLine.Spectre.Output;

namespace Ploch.CommandLine.Spectre.Tests.Output;

/// <summary>
///     Cover for the default implementations on <see cref="IMessageWriter{TMessage}" /> and
///     <see cref="IMessageFormatter{TMessage}" />. They are the contract an implementer gets for free when they
///     implement the generic interface directly instead of deriving from <see cref="TypeMessageWriter{TMessage}" />
///     or <see cref="TypeMessageFormatter{TMessage}" />: type-based handling, and an object-typed entry point that
///     forwards to the strongly-typed one.
/// </summary>
public class TypedMessageHandlerDefaultsTests
{
    [Fact]
    public void IMessageWriter_should_derive_the_message_type_from_its_type_argument()
    {
        IMessageWriter<string> writer = new DirectWriter();

        writer.MessageType.Should().Be<string>("the generic interface supplies the message type without the implementer restating it");
    }

    [Fact]
    public void IMessageWriter_should_accept_only_messages_of_its_type_argument()
    {
        IMessageWriter<string> writer = new DirectWriter();

        writer.CanHandle("text").Should().BeTrue();
        writer.CanHandle(42).Should().BeFalse();
        writer.CanHandle(null).Should().BeFalse();
    }

    [Fact]
    public void IMessageFormatter_should_forward_an_object_typed_message_to_the_strongly_typed_overload()
    {
        IMessageFormatter<string> formatter = new DirectFormatter();

        formatter.GetMessage((object?)"text").Should().Be("formatted:text");
    }

    /// <summary>
    ///     Implements the generic writer interface directly and provides the non-generic <see cref="IMessageHandler" />
    ///     members explicitly, so the generic interface's own defaults are the ones under test.
    /// </summary>
    private sealed class DirectWriter : IMessageWriter<string>
    {
        /// <summary>Deliberately unrelated to <c>string</c>, so a test can tell the two views of the writer apart.</summary>
        Type IMessageHandler.MessageType => typeof(object);

        /// <summary>Deliberately always false, so a test can tell the two views of the writer apart.</summary>
        bool IMessageHandler.CanHandle(object? message) => false;

        public void Write(object? message, IMessageFormatterProcessor? formatterProcessor = null)
        {
            // Not exercised: this test covers the interface defaults, not the writing itself.
        }

        public void Write(string? message, IMessageFormatterProcessor? formatterProcessor = null)
        {
            // Not exercised: this test covers the interface defaults, not the writing itself.
        }
    }

    /// <summary>Implements only the strongly-typed formatting method, leaving the object-typed one to the interface default.</summary>
    private sealed class DirectFormatter : IMessageFormatter<string>
    {
        public Type MessageType => typeof(string);

        public bool CanHandle(object? message) => message is string;

        string IMessageFormatter.GetMessage(object? message, IMessageFormatterProcessor formatterProcessor) =>
            GetMessage((string?)message, formatterProcessor);

        public string GetMessage(string? message, IMessageFormatterProcessor? formatterProcessor = null) => $"formatted:{message}";
    }
}
