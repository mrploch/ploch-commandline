using System.Diagnostics.CodeAnalysis;

namespace Ploch.Common.CommandLine
{
    public interface ICommand
    {
        /// <summary>
        ///     Executes the command.
        /// </summary
        /// <returns>Executed status code integer</returns>
        [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Called dynamically by the CommandLineUtils library")]
        void OnExecute();
    }
}