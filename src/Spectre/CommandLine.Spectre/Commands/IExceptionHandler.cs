namespace Ploch.CommandLine.Spectre.Commands;

public interface IExceptionHandler<out TCommand>
{
    int HandleException(Exception ex);
}
