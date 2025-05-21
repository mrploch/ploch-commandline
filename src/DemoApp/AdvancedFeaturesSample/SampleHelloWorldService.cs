using McMaster.Extensions.CommandLineUtils;

public class SampleHelloWorldService(IConsole console) : ISampleService
{
    public void ExecuteSomeAction()
    {
        ;
        console.WriteLine("Hello World from SampleHelloWorldService");
    }
}