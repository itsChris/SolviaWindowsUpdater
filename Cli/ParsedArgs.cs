using System;
using System.Collections.Generic;

namespace SolviaWindowsUpdater.Cli
{
    /// <summary>
    /// Represents parsed command line arguments.
    /// </summary>
    public class ParsedArgs
    {
        /// <summary>
        /// The main command (verb) specified.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The subcommand (for commands like 'services list').
        /// </summary>
        public string Subcommand { get; set; }

        /// <summary>
        /// Dictionary of option names to their values.
        /// </summary>
        public Dictionary<string, object> Options { get; private set; }

        /// <summary>
        /// List of positional arguments (after command).
        /// </summary>
        public List<string> PositionalArgs { get; private set; }

        /// <summary>
        /// Any parsing errors encountered.
        /// </summary>
        public List<string> ParseErrors { get; private set; }

        /// <summary>
        /// The raw arguments passed to the application.
        /// </summary>
        public string[] RawArgs { get; set; }

        public ParsedArgs()
        {
            Options = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            PositionalArgs = new List<string>();
            ParseErrors = new List<string>();
        }

        /// <summary>
        /// Checks if an option was explicitly provided.
        /// </summary>
        public bool HasOption(string name)
        {
            return Options.ContainsKey(name);
        }

        /// <summary>
        /// Gets an option value with type conversion.
        /// </summary>
        public T GetOption<T>(string name)
        {
            if (!Options.TryGetValue(name, out var value))
            {
                // Return default value from CliSpec if available
                var globalOpt = CliSpec.GetGlobalOption(name);
                if (globalOpt?.DefaultValue != null)
                {
                    return ConvertValue<T>(globalOpt.DefaultValue);
                }

                // Check command-specific options
                if (!string.IsNullOrEmpty(Command))
                {
                    var cmdOpt = CliSpec.GetCommandOption(Command, name);
                    if (cmdOpt?.DefaultValue != null)
                    {
                        return ConvertValue<T>(cmdOpt.DefaultValue);
                    }
                }

                return default(T);
            }

            return ConvertValue<T>(value);
        }

        /// <summary>
        /// Gets an option value or the specified default.
        /// </summary>
        public T GetOptionOrDefault<T>(string name, T defaultValue)
        {
            if (!Options.TryGetValue(name, out var value))
            {
                return defaultValue;
            }
            return ConvertValue<T>(value);
        }

        /// <summary>
        /// Sets an option value.
        /// </summary>
        public void SetOption(string name, object value)
        {
            Options[name] = value;
        }

        private T ConvertValue<T>(object value)
        {
            if (value == null)
                return default(T);

            var targetType = typeof(T);

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            // Already the correct type
            if (value.GetType() == targetType || targetType.IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }

            // Convert from string
            if (value is string strValue)
            {
                if (targetType == typeof(bool))
                {
                    bool result;
                    if (bool.TryParse(strValue, out result))
                        return (T)(object)result;
                    // Handle "1", "0", "yes", "no"
                    if (strValue == "1" || strValue.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)true;
                    if (strValue == "0" || strValue.Equals("no", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)false;
                    return default(T);
                }

                if (targetType == typeof(int))
                {
                    int result;
                    if (int.TryParse(strValue, out result))
                        return (T)(object)result;
                    return default(T);
                }

                if (targetType == typeof(Guid))
                {
                    Guid result;
                    if (Guid.TryParse(strValue, out result))
                        return (T)(object)result;
                    return default(T);
                }

                if (targetType.IsEnum)
                {
                    try
                    {
                        return (T)Enum.Parse(targetType, strValue, true);
                    }
                    catch
                    {
                        return default(T);
                    }
                }

                return (T)(object)strValue;
            }

            // Try general conversion
            try
            {
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Gets the server mode from options.
        /// </summary>
        public ServerMode GetServerMode()
        {
            var server = GetOption<string>("server") ?? "windowsupdate";
            switch (server.ToLowerInvariant())
            {
                case "microsoftupdate":
                    return ServerMode.MicrosoftUpdate;
                default:
                    return ServerMode.WindowsUpdate;
            }
        }

        /// <summary>
        /// Gets the output format from options.
        /// </summary>
        public OutputFormat GetOutputFormat()
        {
            var output = GetOption<string>("output") ?? "table";
            switch (output.ToLowerInvariant())
            {
                case "json":
                    return OutputFormat.Json;
                case "json-full":
                    return OutputFormat.JsonFull;
                default:
                    return OutputFormat.Table;
            }
        }

        /// <summary>
        /// Parses the --select expression and returns the selection type and values.
        /// </summary>
        public bool TryParseSelection(out string selectionType, out string[] values)
        {
            selectionType = null;
            values = null;

            var select = GetOption<string>("select");
            if (string.IsNullOrEmpty(select))
                return false;

            if (select.StartsWith("kb:", StringComparison.OrdinalIgnoreCase))
            {
                selectionType = "kb";
                var kbPart = select.Substring(3);
                values = kbPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return values.Length > 0;
            }

            if (select.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
            {
                selectionType = "index";
                var indexPart = select.Substring(6);
                values = indexPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return values.Length > 0;
            }

            return false;
        }
    }
}
