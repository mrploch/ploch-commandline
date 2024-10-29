using FluentAssertions;
using Xunit;

namespace Ploch.Common.CommandLine.Tests;

public class CommandExecutionIntegrationTests
{
    [Fact]
    public void CommandApplication_should_execute_requested_command_and_provide_parameter_values()
    {
        var testCallback = new TestCallback();
        TestCommandLineApp.AppMain(["testCommand", "--test-arg", "testValue"], testCallback);

        testCallback.ExecuteCalled.Should().BeTrue();
        testCallback.ExecuteArg.Should().Be("testValue");
    }
}