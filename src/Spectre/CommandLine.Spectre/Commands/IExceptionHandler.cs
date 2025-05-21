namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Commands;

public interface IExceptionHandler<out TCommand>
{
    int HandleException(Exception ex);
}
