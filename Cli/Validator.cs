using System.Collections.Generic;
using System.Linq;

namespace SolviaWindowsUpdater.Cli
{
    /// <summary>
    /// Validates parsed arguments against CliSpec rules.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Validates the parsed arguments and returns a list of error messages.
        /// </summary>
        public static List<string> Validate(ParsedArgs args)
        {
            var errors = new List<string>();

            // Add any parsing errors first
            errors.AddRange(args.ParseErrors);

            // Skip validation for help/version commands
            if (args.Command == "help" || args.Command == "version" ||
                args.GetOption<bool>("help") || args.GetOption<bool>("version"))
            {
                return errors;
            }

            // Run all applicable validation rules
            foreach (var rule in CliSpec.ValidationRules)
            {
                if (rule.IsApplicable(args))
                {
                    var error = rule.Validate(args);
                    if (!string.IsNullOrEmpty(error))
                    {
                        errors.Add(error);
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates and returns formatted error output.
        /// </summary>
        public static string FormatErrors(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
                return null;

            var lines = new List<string>
            {
                "Validation errors:",
                ""
            };

            foreach (var error in errors.Distinct())
            {
                lines.Add($"  * {error}");
            }

            lines.Add("");
            lines.Add("Use --help to see valid options and combination rules.");

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Performs full validation including parsing, type conversion, and rule validation.
        /// </summary>
        public static ValidationResult ValidateFull(ParsedArgs args)
        {
            var result = new ValidationResult();

            // Convert option types
            ArgParser.ConvertOptionTypes(args);

            // Validate against rules
            result.Errors = Validate(args);
            result.IsValid = result.Errors.Count == 0;

            return result;
        }
    }

    /// <summary>
    /// Result of validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string GetFormattedErrors()
        {
            return Validator.FormatErrors(Errors);
        }
    }
}
