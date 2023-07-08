using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Security.Principal;
using McMaster.Extensions.CommandLineUtils;

namespace Ploch.Common.CommandLine
{
    public static class CommandValidations
    {
        public static ValidationResult RequireWindowsOS(CommandLineApplication command, ValidationContext context)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ValidationResult($"Command {command.Name} is only supported on Windows OS.");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult RequireAdministrator(CommandLineApplication command, ValidationContext context)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("Only Windows platform is supported.");
            }

            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                return new ValidationResult($"Command {command.Name} has to be executed as an administrator.");
            }

            return ValidationResult.Success;
        }
    }
}