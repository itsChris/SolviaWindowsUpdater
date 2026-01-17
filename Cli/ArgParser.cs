using System;
using System.Collections.Generic;
using System.Linq;

namespace SolviaWindowsUpdater.Cli
{
    /// <summary>
    /// Parses command line arguments into a ParsedArgs structure.
    /// </summary>
    public static class ArgParser
    {
        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        public static ParsedArgs Parse(string[] args)
        {
            var result = new ParsedArgs { RawArgs = args };

            if (args == null || args.Length == 0)
            {
                // No args, default to help
                result.Command = "help";
                return result;
            }

            var i = 0;
            var commandFound = false;

            while (i < args.Length)
            {
                var arg = args[i];

                // Check for help/version flags before command
                if (!commandFound && (arg == "-h" || arg == "--help"))
                {
                    result.Command = "help";
                    result.SetOption("help", true);
                    return result;
                }

                if (!commandFound && (arg == "-v" || arg == "--version"))
                {
                    result.Command = "version";
                    result.SetOption("version", true);
                    return result;
                }

                // Is this an option?
                if (arg.StartsWith("-"))
                {
                    i = ParseOption(args, i, result);
                }
                // Is this the command?
                else if (!commandFound)
                {
                    result.Command = arg.ToLowerInvariant();
                    commandFound = true;
                    i++;

                    // Check for subcommand (e.g., 'services list')
                    if (i < args.Length && !args[i].StartsWith("-"))
                    {
                        var cmd = CliSpec.GetCommand(result.Command);
                        if (cmd?.Subcommands != null)
                        {
                            var potentialSubcmd = args[i].ToLowerInvariant();
                            if (cmd.Subcommands.Any(s => s.Name.Equals(potentialSubcmd, StringComparison.OrdinalIgnoreCase)))
                            {
                                result.Subcommand = potentialSubcmd;
                                i++;
                            }
                        }
                    }
                }
                // Positional argument
                else
                {
                    result.PositionalArgs.Add(arg);
                    i++;
                }
            }

            // Apply default values for options not specified
            ApplyDefaults(result);

            return result;
        }

        private static int ParseOption(string[] args, int index, ParsedArgs result)
        {
            var arg = args[index];
            string optionName;
            string optionValue = null;

            // Handle --option=value format
            var equalsIndex = arg.IndexOf('=');
            if (equalsIndex > 0)
            {
                optionName = arg.Substring(0, equalsIndex).TrimStart('-');
                optionValue = arg.Substring(equalsIndex + 1);
                index++;
            }
            // Handle --option value or -o value format
            else
            {
                optionName = arg.TrimStart('-');
                index++;

                // Look up the option to determine if it's a flag or takes a value
                var optDef = FindOptionDef(optionName, result.Command);

                if (optDef != null && optDef.Type == OptionType.Flag)
                {
                    // Flag - no value needed
                    optionValue = "true";
                }
                else if (index < args.Length && !args[index].StartsWith("-"))
                {
                    // Next arg is the value
                    optionValue = args[index];
                    index++;
                }
                else if (optDef != null && optDef.Type != OptionType.Flag)
                {
                    // Required value not provided
                    result.ParseErrors.Add($"Option --{optionName} requires a value");
                    return index;
                }
                else
                {
                    // Unknown option treated as flag
                    optionValue = "true";
                }
            }

            // Normalize option name (handle short names)
            optionName = NormalizeOptionName(optionName);

            // Store the option
            result.SetOption(optionName, optionValue);

            return index;
        }

        private static OptionDef FindOptionDef(string name, string command)
        {
            // Normalize the name first
            name = NormalizeOptionName(name);

            // Check global options
            var globalOpt = CliSpec.GetGlobalOption(name);
            if (globalOpt != null)
                return globalOpt;

            // Check command-specific options
            if (!string.IsNullOrEmpty(command))
            {
                var cmdOpt = CliSpec.GetCommandOption(command, name);
                if (cmdOpt != null)
                    return cmdOpt;
            }

            return null;
        }

        private static string NormalizeOptionName(string name)
        {
            // Handle short names
            foreach (var opt in CliSpec.GlobalOptions)
            {
                if (!string.IsNullOrEmpty(opt.ShortName) &&
                    opt.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return opt.Name;
                }
            }

            return name;
        }

        private static void ApplyDefaults(ParsedArgs result)
        {
            // Apply global option defaults
            foreach (var opt in CliSpec.GlobalOptions)
            {
                if (opt.DefaultValue != null && !result.HasOption(opt.Name))
                {
                    // Don't add to Options dict - GetOption will return default
                }
            }

            // Apply command-specific option defaults
            if (!string.IsNullOrEmpty(result.Command))
            {
                var cmd = CliSpec.GetCommand(result.Command);
                if (cmd?.Options != null)
                {
                    foreach (var opt in cmd.Options)
                    {
                        if (opt.DefaultValue != null && !result.HasOption(opt.Name))
                        {
                            // Don't add to Options dict - GetOption will return default
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts parsed option values to their correct types based on CliSpec.
        /// </summary>
        public static void ConvertOptionTypes(ParsedArgs result)
        {
            var optionsToConvert = new Dictionary<string, object>(result.Options);

            foreach (var kvp in optionsToConvert)
            {
                var optDef = FindOptionDef(kvp.Key, result.Command);
                if (optDef == null)
                    continue;

                var strValue = kvp.Value as string;
                if (strValue == null)
                    continue;

                object converted = null;

                switch (optDef.Type)
                {
                    case OptionType.Flag:
                        bool boolResult;
                        if (bool.TryParse(strValue, out boolResult))
                            converted = boolResult;
                        else if (strValue == "1")
                            converted = true;
                        else if (strValue == "0")
                            converted = false;
                        break;

                    case OptionType.Int:
                        int intResult;
                        if (int.TryParse(strValue, out intResult))
                            converted = intResult;
                        else
                            result.ParseErrors.Add($"Option --{kvp.Key} requires an integer value (got: '{strValue}')");
                        break;

                    case OptionType.Enum:
                        if (optDef.AllowedValues != null)
                        {
                            var match = optDef.AllowedValues
                                .FirstOrDefault(v => v.Equals(strValue, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                                converted = match;
                            else
                                result.ParseErrors.Add($"Option --{kvp.Key} must be one of: {string.Join(", ", optDef.AllowedValues)} (got: '{strValue}')");
                        }
                        break;

                    case OptionType.String:
                        converted = strValue;
                        break;
                }

                if (converted != null)
                {
                    result.Options[kvp.Key] = converted;
                }
            }
        }
    }
}
