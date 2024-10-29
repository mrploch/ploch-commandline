namespace Ploch.Common.CommandLine.Tests;

public class TestCallback
{
    public bool ExecuteCalled { get; set; }

    public string? ExecuteArg { get; set; }

    public void Execute(string testArg)
    {
        ExecuteCalled = true;
        ExecuteArg = testArg;
    }
}