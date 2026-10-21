using Ploch.CommandLine.Spectre.Commands;

namespace Ploch.CommandLine.Spectre.Tests.Commands;

public class ExceptionHandlerFailedExceptionTests
{
    [Fact]
    public void Constructor_should_describe_both_exceptions_and_keep_them()
    {
        var original = new InvalidOperationException("the command failed");
        var handlerException = new ArgumentException("the handler failed");

        var exception = new ExceptionHandlerFailedException(original, handlerException);

        exception.Message.Should().Be("The exception handler threw ArgumentException while handling InvalidOperationException: the handler failed");
        exception.Original.Should().BeSameAs(original);
        exception.InnerException.Should().BeSameAs(handlerException);
    }

    [Fact]
    public void Standard_constructors_should_leave_the_original_exception_unset()
    {
        var inner = new ArgumentException("inner");

        new ExceptionHandlerFailedException().Original.Should().BeNull();
        new ExceptionHandlerFailedException("message").Should().Match<ExceptionHandlerFailedException>(e => e.Message == "message" && e.Original == null);
        new ExceptionHandlerFailedException("message", inner).Should().Match<ExceptionHandlerFailedException>(e => e.InnerException == inner && e.Original == null);
    }

    [Fact]
    public void Invoke_should_wrap_what_the_handler_throws_and_pass_through_what_it_returns()
    {
        var original = new InvalidOperationException("the command failed");
        var returning = new DelegateExceptionHandler(_ => 5);
        var throwing = new DelegateExceptionHandler(_ => throw new ArgumentException("the handler failed"));

        ExceptionHandlerInvoker.Invoke(returning, original).Should().Be(5);
        var act = () => ExceptionHandlerInvoker.Invoke(throwing, original);

        act.Should().Throw<ExceptionHandlerFailedException>()
           .Which.Should().Match<ExceptionHandlerFailedException>(e => e.Original == original && e.InnerException is ArgumentException);
    }

    private sealed class DelegateExceptionHandler(Func<Exception, int> handle) : IExceptionHandler
    {
        public int HandleException(Exception ex) => handle(ex);
    }
}
