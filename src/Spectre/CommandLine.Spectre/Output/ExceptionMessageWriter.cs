using Ploch.Tools.SystemProfiles.Core;

namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public class ExceptionMessageWriter(IOutput output) : TypeMessageWriter<Exception>
{
    public override void Write(Exception? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        output.WriteException(message);
    }
}
