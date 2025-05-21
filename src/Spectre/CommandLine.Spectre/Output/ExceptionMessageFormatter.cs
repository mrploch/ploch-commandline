namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;
//
// public class BaseExceptionMessageWriter<TException>(IOutput output) : TypeMessageWriter<TException> where TException : Exception
// {
//     public override void Write(TException? message)
//     {
//         message.NotNull();
//         
//         output.Write("[red] Error:")
//     }
// }

public class ExceptionMessageFormatter : BaseExceptionMessageFormatter<Exception>
{ }
