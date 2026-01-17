using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolviaWindowsUpdater.Cli
{
    /// <summary>
    /// Generates help text from CliSpec.
    /// </summary>
    public static class HelpGenerator
    {
        private const int IndentSize = 2;
        private const int OptionColumnWidth = 35;

        /// <summary>
        /// Generates the full help output.
        /// </summary>
        public static string GenerateFullHelp()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"{CliSpec.AppName} v{CliSpec.Version}");
            sb.AppendLine(CliSpec.Description);
            sb.AppendLine();

            // Usage
            sb.AppendLine("USAGE:");
            sb.AppendLine($"  {CliSpec.AppName.ToLower()} <command> [options]");
            sb.AppendLine();

            // Commands
            sb.AppendLine("COMMANDS:");
            foreach (var cmd in CliSpec.Commands)
            {
                var cmdLine = $"  {cmd.Name.PadRight(15)} {cmd.Description}";
                if (cmd.RequiresAdmin)
                    cmdLine += " [admin]";
                sb.AppendLine(cmdLine);

                if (cmd.Subcommands != null)
                {
                    foreach (var sub in cmd.Subcommands)
                    {
                        var subLine = $"    {sub.Name.PadRight(13)} {sub.Description}";
                        if (sub.RequiresAdmin)
                            subLine += " [admin]";
                        sb.AppendLine(subLine);
                    }
                }
            }
            sb.AppendLine();

            // Global Options
            sb.AppendLine("GLOBAL OPTIONS:");
            foreach (var opt in CliSpec.GlobalOptions)
            {
                AppendOption(sb, opt);
            }
            sb.AppendLine();

            // Server Modes
            sb.AppendLine("SERVER MODES:");
            sb.AppendLine("  windowsupdate    Windows Update service (default)");
            sb.AppendLine("  microsoftupdate  Microsoft Update service (includes Office, drivers)");
            sb.AppendLine();
            sb.AppendLine("  Use --service-id <guid> with 'microsoftupdate' to target a specific service.");
            sb.AppendLine();

            // Selection Syntax
            sb.AppendLine("SELECTION SYNTAX (for --select):");
            sb.AppendLine("  --all                      Select all updates from search");
            sb.AppendLine("  --select kb:KB5001234      Select by KB article ID");
            sb.AppendLine("  --select kb:KB123,KB456    Select multiple KBs (comma-separated)");
            sb.AppendLine("  --select index:1,2,3       Select by index from search results");
            sb.AppendLine();

            // Combination Rules
            sb.AppendLine("COMBINATION RULES:");
            sb.AppendLine();
            AppendCombinationRules(sb);
            sb.AppendLine();

            // Examples
            sb.AppendLine("EXAMPLES:");
            sb.AppendLine();
            sb.AppendLine("  Search for available updates:");
            sb.AppendLine("    SolviaWindowsUpdater search");
            sb.AppendLine("    SolviaWindowsUpdater search --criteria \"IsInstalled=0 AND Type='Software'\"");
            sb.AppendLine("    SolviaWindowsUpdater search --server microsoftupdate --output json");
            sb.AppendLine();
            sb.AppendLine("  Download updates:");
            sb.AppendLine("    SolviaWindowsUpdater download --all");
            sb.AppendLine("    SolviaWindowsUpdater download --select kb:KB5001234");
            sb.AppendLine("    SolviaWindowsUpdater download --select index:1,2,3 --whatif");
            sb.AppendLine();
            sb.AppendLine("  Install updates:");
            sb.AppendLine("    SolviaWindowsUpdater install --all --accept-eulas");
            sb.AppendLine("    SolviaWindowsUpdater install --select kb:KB5001234 --accept-eulas --noreboot");
            sb.AppendLine();
            sb.AppendLine("  Uninstall updates:");
            sb.AppendLine("    SolviaWindowsUpdater uninstall --select kb:KB5001234");
            sb.AppendLine();
            sb.AppendLine("  View history:");
            sb.AppendLine("    SolviaWindowsUpdater history");
            sb.AppendLine("    SolviaWindowsUpdater history --count 100 --output json-full");
            sb.AppendLine();
            sb.AppendLine("  Manage services:");
            sb.AppendLine("    SolviaWindowsUpdater services list");
            sb.AppendLine("    SolviaWindowsUpdater services remove --service-id <guid>");
            sb.AppendLine();
            sb.AppendLine("  Check status:");
            sb.AppendLine("    SolviaWindowsUpdater status");

            return sb.ToString();
        }

        /// <summary>
        /// Generates help for a specific command.
        /// </summary>
        public static string GenerateCommandHelp(string commandName)
        {
            var cmd = CliSpec.GetCommand(commandName);
            if (cmd == null)
            {
                return $"Unknown command: {commandName}\n\nUse --help to see available commands.";
            }

            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"{CliSpec.AppName} {cmd.Name}");
            sb.AppendLine();
            sb.AppendLine($"  {cmd.Description}");
            if (cmd.RequiresAdmin)
                sb.AppendLine("  [Requires administrator privileges]");
            sb.AppendLine();

            // Usage
            sb.AppendLine("USAGE:");
            if (cmd.Subcommands != null)
            {
                sb.AppendLine($"  {CliSpec.AppName.ToLower()} {cmd.Name} <subcommand> [options]");
            }
            else
            {
                sb.AppendLine($"  {CliSpec.AppName.ToLower()} {cmd.Name} [options]");
            }
            sb.AppendLine();

            // Subcommands
            if (cmd.Subcommands != null)
            {
                sb.AppendLine("SUBCOMMANDS:");
                foreach (var sub in cmd.Subcommands)
                {
                    var subLine = $"  {sub.Name.PadRight(15)} {sub.Description}";
                    if (sub.RequiresAdmin)
                        subLine += " [admin]";
                    sb.AppendLine(subLine);
                }
                sb.AppendLine();
            }

            // Command-specific options
            if (cmd.Options != null && cmd.Options.Count > 0)
            {
                sb.AppendLine("OPTIONS:");
                foreach (var opt in cmd.Options)
                {
                    AppendOption(sb, opt);
                }
                sb.AppendLine();
            }

            // Applicable global options
            var globalOpts = CliSpec.GlobalOptions.Where(o =>
            {
                if (o.ForbiddenCommands != null && o.ForbiddenCommands.Contains(cmd.Name, StringComparer.OrdinalIgnoreCase))
                    return false;
                if (o.AllowedCommands != null && !o.AllowedCommands.Contains(cmd.Name, StringComparer.OrdinalIgnoreCase))
                    return false;
                return true;
            }).ToList();

            if (globalOpts.Count > 0)
            {
                sb.AppendLine("GLOBAL OPTIONS:");
                foreach (var opt in globalOpts)
                {
                    AppendOption(sb, opt);
                }
                sb.AppendLine();
            }

            // Examples
            if (cmd.Examples != null && cmd.Examples.Length > 0)
            {
                sb.AppendLine("EXAMPLES:");
                foreach (var example in cmd.Examples)
                {
                    sb.AppendLine($"  {CliSpec.AppName.ToLower()} {example}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates version output.
        /// </summary>
        public static string GenerateVersion()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{CliSpec.AppName} v{CliSpec.Version}");
            sb.AppendLine(CliSpec.Description);
            sb.AppendLine();
            sb.AppendLine($"  .NET Framework: {Environment.Version}");
            sb.AppendLine($"  OS: {Environment.OSVersion}");
            return sb.ToString();
        }

        private static void AppendOption(StringBuilder sb, OptionDef opt)
        {
            var usage = opt.GetUsageString();
            var line = $"  {usage.PadRight(OptionColumnWidth)} {opt.Description}";
            sb.AppendLine(line);

            // Default value
            if (opt.DefaultValue != null && opt.Type != OptionType.Flag)
            {
                sb.AppendLine($"  {new string(' ', OptionColumnWidth)} Default: {opt.DefaultValue}");
            }

            // Constraints
            if (opt.MinValue.HasValue && opt.MaxValue.HasValue)
            {
                sb.AppendLine($"  {new string(' ', OptionColumnWidth)} Range: {opt.MinValue} - {opt.MaxValue}");
            }

            // Allowed values
            if (opt.AllowedValues != null && opt.AllowedValues.Length > 0 && opt.Type == OptionType.Enum)
            {
                // Already shown in usage string
            }

            // Command restrictions
            if (opt.AllowedCommands != null && opt.AllowedCommands.Length > 0)
            {
                sb.AppendLine($"  {new string(' ', OptionColumnWidth)} Only for: {string.Join(", ", opt.AllowedCommands)}");
            }
        }

        private static void AppendCombinationRules(StringBuilder sb)
        {
            var rules = new List<string>
            {
                "1. Exactly one command must be provided (search, download, install, uninstall, history, services, status)",
                "",
                "2. --service-id can only be used with --server microsoftupdate",
                "",
                "3. --whatif is only allowed for: download, install, uninstall",
                "",
                "4. download and install commands require --all OR --select <expression>",
                "   - Cannot use both --all and --select together",
                "",
                "5. uninstall command requires --select kb:KBxxxx",
                "",
                "6. --select syntax:",
                "   - kb:KBxxxx           Single KB",
                "   - kb:KB1,KB2,KB3      Multiple KBs (comma-separated)",
                "   - index:1,2,3         Select by index from search results",
                "",
                "7. --force is only allowed for: download, install, uninstall",
                "",
                "8. --accept-eulas is only allowed for: install",
                "   - If not provided and an update requires EULA acceptance, installation will fail",
                "",
                "9. --noreboot is only allowed for: install, uninstall",
                "   - Default behavior: prompt for reboot if required",
                "   - With --noreboot: suppress prompt, show reboot-required message",
                "",
                "10. --output is only allowed for: search, history",
                "    - table:     Human-readable table format (default)",
                "    - json:      Minimal JSON output",
                "    - json-full: Verbose JSON with all properties",
                "",
                "11. --include-hidden is only allowed for: search",
                "",
                "12. --max-results is only allowed for: search (range: 1-500)",
                "",
                "13. --criteria is allowed for: search, download, install",
                "    - Default: IsInstalled=0",
                "",
                "14. services command requires subcommand: list or remove",
                "    - services remove requires --service-id <guid>",
                "",
                "15. Integer ranges:",
                "    - --timeout-seconds: 1 - 3600",
                "    - --max-results: 1 - 500",
                "    - --count: 1 - 500",
                "    - --start-index: >= 0"
            };

            foreach (var rule in rules)
            {
                sb.AppendLine($"  {rule}");
            }
        }
    }
}
