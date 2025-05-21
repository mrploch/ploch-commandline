namespace Ploch.CommandLine.Spectre.Output;

public class ExceptionMessageWriter(IOutput output) : TypeMessageWriter<Exception>
{
    public override void Write(Exception? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        output.WriteException(message);
    }
}
