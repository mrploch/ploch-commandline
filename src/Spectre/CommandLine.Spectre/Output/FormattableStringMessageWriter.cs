using Ploch.Tools.SystemProfiles.Core;

namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;

public class FormattableStringMessageWriter(IOutput output) : TypeMessageWriter<FormattableString>
{
    public override void Write(FormattableString? message, IMessageFormatterProcessor? formatterProcessor = null)
    {
        var messageText = formatterProcessor.GetMessageText(message);

        if (messageText is null)
        {
            return;
        }

        output.MarkupInterpolated(messageText);
    }
}
