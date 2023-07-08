using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace Ploch.Common.CommandLine
{
    public delegate ValidationResult PreExecuteCommandValidator(CommandLineApplication command, ValidationContext context);

    public class DelegatedCommandValidator : ICommandValidator
    {
        private readonly PreExecuteCommandValidator _validator;

        public DelegatedCommandValidator(PreExecuteCommandValidator validator)
        {
            _validator = validator;
        }

        public ValidationResult GetValidationResult(CommandLineApplication command, ValidationContext context)
        {
            return _validator(command, context);
        }
    }
}