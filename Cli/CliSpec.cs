using System;
using System.Collections.Generic;
using System.Linq;

namespace SolviaWindowsUpdater.Cli
{
    #region Enums

    public enum OptionType
    {
        Flag,       // Boolean switch, no value
        String,     // String value
        Int,        // Integer value
        Enum        // Enumerated value from allowed list
    }

    public enum ServerMode
    {
        WindowsUpdate,
        MicrosoftUpdate
    }

    public enum OutputFormat
    {
        Table,
        Json,
        JsonFull
    }

    #endregion

    #region Option Definition

    /// <summary>
    /// Defines a single CLI option.
    /// </summary>
    public class OptionDef
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public OptionType Type { get; set; }
        public object DefaultValue { get; set; }
        public bool Required { get; set; }
        public string[] AllowedValues { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string[] AllowedCommands { get; set; }  // null = all commands
        public string[] ForbiddenCommands { get; set; } // Commands where this option is forbidden
        public string Example { get; set; }

        public string GetUsageString()
        {
            var shortPart = !string.IsNullOrEmpty(ShortName) ? $"-{ShortName}|" : "";
            var namePart = $"--{Name}";
            var valuePart = Type == OptionType.Flag ? "" : $" <{GetValueHint()}>";
            return $"{shortPart}{namePart}{valuePart}";
        }

        private string GetValueHint()
        {
            if (AllowedValues != null && AllowedValues.Length > 0)
            {
                return string.Join("|", AllowedValues);
            }
            switch (Type)
            {
                case OptionType.Int:
                    if (MinValue.HasValue && MaxValue.HasValue)
                        return $"int:{MinValue}-{MaxValue}";
                    return "int";
                case OptionType.String:
                    return "string";
                default:
                    return "value";
            }
        }
    }

    #endregion

    #region Command Definition

    /// <summary>
    /// Defines a CLI command (verb).
    /// </summary>
    public class CommandDef
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Aliases { get; set; }
        public bool RequiresAdmin { get; set; }
        public string[] Examples { get; set; }
        public List<OptionDef> Options { get; set; } = new List<OptionDef>();
        public List<CommandDef> Subcommands { get; set; }
    }

    #endregion

    #region Validation Rule

    /// <summary>
    /// Defines a validation rule for option combinations.
    /// </summary>
    public class ValidationRule
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public Func<ParsedArgs, bool> IsApplicable { get; set; }
        public Func<ParsedArgs, string> Validate { get; set; } // Returns error message or null if valid
    }

    #endregion

    #region CliSpec - Single Source of Truth

    /// <summary>
    /// The single source of truth for all CLI commands, options, defaults, and validation rules.
    /// </summary>
    public static class CliSpec
    {
        public const string AppName = "SolviaWindowsUpdater";
        public const string Version = "1.0.0";
        public const string Description = "Windows Update Agent CLI Tool";
        public const string DefaultLogPath = @"%SystemDrive%\Solvia\Logs\Solvia.WuaCli.log";

        #region Global Options

        public static readonly List<OptionDef> GlobalOptions = new List<OptionDef>
        {
            new OptionDef
            {
                Name = "help",
                ShortName = "h",
                Description = "Show help information",
                Type = OptionType.Flag,
                DefaultValue = false
            },
            new OptionDef
            {
                Name = "version",
                ShortName = "v",
                Description = "Show version information",
                Type = OptionType.Flag,
                DefaultValue = false
            },
            new OptionDef
            {
                Name = "log-path",
                Description = "Path to log file",
                Type = OptionType.String,
                DefaultValue = DefaultLogPath,
                Example = @"--log-path C:\Logs\wu.log"
            },
            new OptionDef
            {
                Name = "log-level",
                Description = "Minimum log level",
                Type = OptionType.Enum,
                DefaultValue = "Info",
                AllowedValues = new[] { "Trace", "Debug", "Info", "Warn", "Error" }
            },
            new OptionDef
            {
                Name = "timeout-seconds",
                Description = "Operation timeout in seconds",
                Type = OptionType.Int,
                DefaultValue = 300,
                MinValue = 1,
                MaxValue = 3600
            },
            new OptionDef
            {
                Name = "server",
                Description = "Windows Update server to use",
                Type = OptionType.Enum,
                DefaultValue = "windowsupdate",
                AllowedValues = new[] { "windowsupdate", "microsoftupdate" },
                ForbiddenCommands = new[] { "help", "version" }
            },
            new OptionDef
            {
                Name = "service-id",
                Description = "Specific service ID (GUID) to target",
                Type = OptionType.String,
                AllowedCommands = new[] { "search", "download", "install" },
                Example = "--service-id 7971f918-a847-4430-9279-4a52d1efe18d"
            },
            new OptionDef
            {
                Name = "accept-eulas",
                Description = "Automatically accept EULAs for updates",
                Type = OptionType.Flag,
                DefaultValue = false,
                AllowedCommands = new[] { "install" }
            },
            new OptionDef
            {
                Name = "whatif",
                Description = "Show what would happen without making changes",
                Type = OptionType.Flag,
                DefaultValue = false,
                AllowedCommands = new[] { "download", "install", "uninstall" }
            },
            new OptionDef
            {
                Name = "noreboot",
                Description = "Suppress reboot prompt; only show reboot-required message",
                Type = OptionType.Flag,
                DefaultValue = false,
                AllowedCommands = new[] { "install", "uninstall" }
            }
        };

        #endregion

        #region Commands

        public static readonly List<CommandDef> Commands = new List<CommandDef>
        {
            new CommandDef
            {
                Name = "search",
                Description = "Search for updates by criteria and show results",
                RequiresAdmin = false,
                Examples = new[]
                {
                    "search",
                    "search --criteria \"IsInstalled=0 AND Type='Software'\"",
                    "search --include-hidden --max-results 100",
                    "search --output json --server microsoftupdate"
                },
                Options = new List<OptionDef>
                {
                    new OptionDef
                    {
                        Name = "criteria",
                        Description = "Windows Update search criteria",
                        Type = OptionType.String,
                        DefaultValue = "IsInstalled=0",
                        Example = "--criteria \"IsInstalled=0 AND Type='Software'\""
                    },
                    new OptionDef
                    {
                        Name = "include-hidden",
                        Description = "Include hidden updates in search results",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    },
                    new OptionDef
                    {
                        Name = "max-results",
                        Description = "Maximum number of results to return",
                        Type = OptionType.Int,
                        DefaultValue = 50,
                        MinValue = 1,
                        MaxValue = 500
                    },
                    new OptionDef
                    {
                        Name = "output",
                        Description = "Output format",
                        Type = OptionType.Enum,
                        DefaultValue = "table",
                        AllowedValues = new[] { "table", "json", "json-full" }
                    }
                }
            },
            new CommandDef
            {
                Name = "download",
                Description = "Download selected updates",
                RequiresAdmin = true,
                Examples = new[]
                {
                    "download --all",
                    "download --select kb:KB5001234",
                    "download --select kb:KB5001234,KB5001235 --force",
                    "download --criteria \"IsInstalled=0\" --select index:1,2,3",
                    "download --all --whatif"
                },
                Options = new List<OptionDef>
                {
                    new OptionDef
                    {
                        Name = "all",
                        Description = "Select all updates from search results",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    },
                    new OptionDef
                    {
                        Name = "select",
                        Description = "Selection expression: kb:KBxxxx,... or index:1,2,...",
                        Type = OptionType.String,
                        Example = "--select kb:KB5001234,KB5001235"
                    },
                    new OptionDef
                    {
                        Name = "criteria",
                        Description = "Windows Update search criteria",
                        Type = OptionType.String,
                        DefaultValue = "IsInstalled=0"
                    },
                    new OptionDef
                    {
                        Name = "force",
                        Description = "Force re-download even if already cached",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    }
                }
            },
            new CommandDef
            {
                Name = "install",
                Description = "Install selected updates",
                RequiresAdmin = true,
                Examples = new[]
                {
                    "install --all --accept-eulas",
                    "install --select kb:KB5001234 --accept-eulas",
                    "install --all --accept-eulas --noreboot",
                    "install --select index:1,2 --whatif"
                },
                Options = new List<OptionDef>
                {
                    new OptionDef
                    {
                        Name = "all",
                        Description = "Select all updates from search results",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    },
                    new OptionDef
                    {
                        Name = "select",
                        Description = "Selection expression: kb:KBxxxx,... or index:1,2,...",
                        Type = OptionType.String,
                        Example = "--select kb:KB5001234,KB5001235"
                    },
                    new OptionDef
                    {
                        Name = "criteria",
                        Description = "Windows Update search criteria",
                        Type = OptionType.String,
                        DefaultValue = "IsInstalled=0"
                    },
                    new OptionDef
                    {
                        Name = "force",
                        Description = "Force reinstallation",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    }
                }
            },
            new CommandDef
            {
                Name = "uninstall",
                Description = "Uninstall selected updates",
                RequiresAdmin = true,
                Examples = new[]
                {
                    "uninstall --select kb:KB5001234",
                    "uninstall --select kb:KB5001234 --noreboot",
                    "uninstall --select kb:KB5001234 --whatif"
                },
                Options = new List<OptionDef>
                {
                    new OptionDef
                    {
                        Name = "select",
                        Description = "Selection expression: kb:KBxxxx,...",
                        Type = OptionType.String,
                        Required = true,
                        Example = "--select kb:KB5001234"
                    },
                    new OptionDef
                    {
                        Name = "force",
                        Description = "Force uninstallation",
                        Type = OptionType.Flag,
                        DefaultValue = false
                    }
                }
            },
            new CommandDef
            {
                Name = "history",
                Description = "Query update installation history",
                RequiresAdmin = false,
                Examples = new[]
                {
                    "history",
                    "history --count 100",
                    "history --start-index 50 --count 25",
                    "history --output json-full"
                },
                Options = new List<OptionDef>
                {
                    new OptionDef
                    {
                        Name = "start-index",
                        Description = "Starting index in history",
                        Type = OptionType.Int,
                        DefaultValue = 0,
                        MinValue = 0,
                        MaxValue = int.MaxValue
                    },
                    new OptionDef
                    {
                        Name = "count",
                        Description = "Number of history entries to retrieve (0 or omit for all)",
                        Type = OptionType.Int,
                        DefaultValue = 0,
                        MinValue = 0,
                        MaxValue = int.MaxValue
                    },
                    new OptionDef
                    {
                        Name = "output",
                        Description = "Output format",
                        Type = OptionType.Enum,
                        DefaultValue = "table",
                        AllowedValues = new[] { "table", "json", "json-full" }
                    }
                }
            },
            new CommandDef
            {
                Name = "services",
                Description = "List or remove update services",
                RequiresAdmin = false,  // Depends on subcommand
                Examples = new[]
                {
                    "services list",
                    "services remove --service-id 7971f918-a847-4430-9279-4a52d1efe18d"
                },
                Subcommands = new List<CommandDef>
                {
                    new CommandDef
                    {
                        Name = "list",
                        Description = "List registered update services",
                        RequiresAdmin = false,
                        Examples = new[] { "services list" }
                    },
                    new CommandDef
                    {
                        Name = "remove",
                        Description = "Remove an update service",
                        RequiresAdmin = true,
                        Examples = new[] { "services remove --service-id 7971f918-a847-4430-9279-4a52d1efe18d" },
                        Options = new List<OptionDef>
                        {
                            new OptionDef
                            {
                                Name = "service-id",
                                Description = "Service ID (GUID) to remove",
                                Type = OptionType.String,
                                Required = true
                            }
                        }
                    }
                }
            },
            new CommandDef
            {
                Name = "status",
                Description = "Show system reboot-required status",
                RequiresAdmin = false,
                Examples = new[] { "status" }
            },
            new CommandDef
            {
                Name = "help",
                Description = "Show help information",
                RequiresAdmin = false,
                Examples = new[] { "help", "help search", "help install" }
            },
            new CommandDef
            {
                Name = "version",
                Description = "Show version information",
                RequiresAdmin = false,
                Examples = new[] { "version" }
            }
        };

        #endregion

        #region Validation Rules

        public static readonly List<ValidationRule> ValidationRules = new List<ValidationRule>
        {
            // Rule 1: Exactly one command must be provided
            new ValidationRule
            {
                Id = "CMD001",
                Description = "Exactly one command must be provided",
                IsApplicable = args => true,
                Validate = args =>
                {
                    if (string.IsNullOrEmpty(args.Command))
                        return "No command specified. Use --help to see available commands.";
                    return null;
                }
            },

            // Rule 2: Command must be valid
            new ValidationRule
            {
                Id = "CMD002",
                Description = "Command must be a recognized command",
                IsApplicable = args => !string.IsNullOrEmpty(args.Command),
                Validate = args =>
                {
                    var validCommands = Commands.Select(c => c.Name).ToList();
                    if (!validCommands.Contains(args.Command.ToLowerInvariant()))
                        return $"Unknown command '{args.Command}'. Valid commands: {string.Join(", ", validCommands)}";
                    return null;
                }
            },

            // Rule 3: --service-id requires --server microsoftupdate
            new ValidationRule
            {
                Id = "OPT001",
                Description = "--service-id is only allowed with --server microsoftupdate",
                IsApplicable = args => args.HasOption("service-id"),
                Validate = args =>
                {
                    var server = args.GetOption<string>("server") ?? "windowsupdate";
                    if (server.ToLowerInvariant() != "microsoftupdate")
                        return "--service-id can only be used when --server is 'microsoftupdate'";
                    return null;
                }
            },

            // Rule 4: --whatif only for download/install/uninstall
            new ValidationRule
            {
                Id = "OPT002",
                Description = "--whatif is only allowed for download, install, uninstall commands",
                IsApplicable = args => args.GetOption<bool>("whatif"),
                Validate = args =>
                {
                    var allowed = new[] { "download", "install", "uninstall" };
                    if (!allowed.Contains(args.Command?.ToLowerInvariant()))
                        return $"--whatif is only allowed for: {string.Join(", ", allowed)}";
                    return null;
                }
            },

            // Rule 5: download/install require --all or --select
            new ValidationRule
            {
                Id = "SEL001",
                Description = "download and install commands require --all or --select",
                IsApplicable = args => new[] { "download", "install" }.Contains(args.Command?.ToLowerInvariant()),
                Validate = args =>
                {
                    var hasAll = args.GetOption<bool>("all");
                    var hasSelect = args.HasOption("select");
                    if (!hasAll && !hasSelect)
                        return $"{args.Command} requires either --all or --select <expression>";
                    if (hasAll && hasSelect)
                        return "Cannot use both --all and --select; choose one";
                    return null;
                }
            },

            // Rule 6: uninstall requires --select
            new ValidationRule
            {
                Id = "SEL002",
                Description = "uninstall command requires --select",
                IsApplicable = args => args.Command?.ToLowerInvariant() == "uninstall",
                Validate = args =>
                {
                    if (!args.HasOption("select"))
                        return "uninstall requires --select kb:KBxxxx";
                    return null;
                }
            },

            // Rule 7: --select format validation
            new ValidationRule
            {
                Id = "SEL003",
                Description = "--select must be in format 'kb:KBxxxx,...' or 'index:1,2,...'",
                IsApplicable = args => args.HasOption("select"),
                Validate = args =>
                {
                    var select = args.GetOption<string>("select");
                    if (string.IsNullOrWhiteSpace(select))
                        return "--select cannot be empty";

                    if (!select.StartsWith("kb:", StringComparison.OrdinalIgnoreCase) &&
                        !select.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
                        return "--select must start with 'kb:' or 'index:' (e.g., --select kb:KB5001234 or --select index:1,2,3)";

                    return null;
                }
            },

            // Rule 8: --force only for download/install/uninstall
            new ValidationRule
            {
                Id = "OPT003",
                Description = "--force is only allowed for download, install, uninstall commands",
                IsApplicable = args => args.GetOption<bool>("force"),
                Validate = args =>
                {
                    var allowed = new[] { "download", "install", "uninstall" };
                    if (!allowed.Contains(args.Command?.ToLowerInvariant()))
                        return $"--force is only allowed for: {string.Join(", ", allowed)}";
                    return null;
                }
            },

            // Rule 9: --accept-eulas only for install
            new ValidationRule
            {
                Id = "OPT004",
                Description = "--accept-eulas is only allowed for install command",
                IsApplicable = args => args.GetOption<bool>("accept-eulas"),
                Validate = args =>
                {
                    if (args.Command?.ToLowerInvariant() != "install")
                        return "--accept-eulas is only allowed for the install command";
                    return null;
                }
            },

            // Rule 10: --noreboot only for install/uninstall
            new ValidationRule
            {
                Id = "OPT005",
                Description = "--noreboot is only allowed for install, uninstall commands",
                IsApplicable = args => args.GetOption<bool>("noreboot"),
                Validate = args =>
                {
                    var allowed = new[] { "install", "uninstall" };
                    if (!allowed.Contains(args.Command?.ToLowerInvariant()))
                        return $"--noreboot is only allowed for: {string.Join(", ", allowed)}";
                    return null;
                }
            },

            // Rule 11: --output only for search/history
            new ValidationRule
            {
                Id = "OPT006",
                Description = "--output is only allowed for search, history commands",
                IsApplicable = args => args.HasOption("output"),
                Validate = args =>
                {
                    var allowed = new[] { "search", "history" };
                    if (!allowed.Contains(args.Command?.ToLowerInvariant()))
                        return $"--output is only allowed for: {string.Join(", ", allowed)}";
                    return null;
                }
            },

            // Rule 12: --include-hidden only for search
            new ValidationRule
            {
                Id = "OPT007",
                Description = "--include-hidden is only allowed for search command",
                IsApplicable = args => args.GetOption<bool>("include-hidden"),
                Validate = args =>
                {
                    if (args.Command?.ToLowerInvariant() != "search")
                        return "--include-hidden is only allowed for the search command";
                    return null;
                }
            },

            // Rule 13: --max-results only for search
            new ValidationRule
            {
                Id = "OPT008",
                Description = "--max-results is only allowed for search command",
                IsApplicable = args => args.HasOption("max-results") && args.GetOption<int>("max-results") != 50,
                Validate = args =>
                {
                    if (args.Command?.ToLowerInvariant() != "search")
                        return "--max-results is only allowed for the search command";
                    return null;
                }
            },

            // Rule 14: Integer range validation for timeout-seconds
            new ValidationRule
            {
                Id = "VAL001",
                Description = "--timeout-seconds must be between 1 and 3600",
                IsApplicable = args => args.HasOption("timeout-seconds"),
                Validate = args =>
                {
                    var value = args.GetOption<int>("timeout-seconds");
                    if (value < 1 || value > 3600)
                        return "--timeout-seconds must be between 1 and 3600";
                    return null;
                }
            },

            // Rule 15: Integer range validation for max-results
            new ValidationRule
            {
                Id = "VAL002",
                Description = "--max-results must be between 1 and 500",
                IsApplicable = args => args.HasOption("max-results"),
                Validate = args =>
                {
                    var value = args.GetOption<int>("max-results");
                    if (value < 1 || value > 500)
                        return "--max-results must be between 1 and 500";
                    return null;
                }
            },

            // Rule 16: Integer range validation for count
            new ValidationRule
            {
                Id = "VAL003",
                Description = "--count must be >= 0 (0 means all entries)",
                IsApplicable = args => args.HasOption("count"),
                Validate = args =>
                {
                    var value = args.GetOption<int>("count");
                    if (value < 0)
                        return "--count must be >= 0 (use 0 for all entries)";
                    return null;
                }
            },

            // Rule 17: start-index must be non-negative
            new ValidationRule
            {
                Id = "VAL004",
                Description = "--start-index must be >= 0",
                IsApplicable = args => args.HasOption("start-index"),
                Validate = args =>
                {
                    var value = args.GetOption<int>("start-index");
                    if (value < 0)
                        return "--start-index must be >= 0";
                    return null;
                }
            },

            // Rule 18: services subcommand validation
            new ValidationRule
            {
                Id = "SVC001",
                Description = "services command requires a subcommand (list or remove)",
                IsApplicable = args => args.Command?.ToLowerInvariant() == "services",
                Validate = args =>
                {
                    if (string.IsNullOrEmpty(args.Subcommand))
                        return "services command requires a subcommand: list or remove";
                    var valid = new[] { "list", "remove" };
                    if (!valid.Contains(args.Subcommand.ToLowerInvariant()))
                        return $"Invalid services subcommand '{args.Subcommand}'. Valid: {string.Join(", ", valid)}";
                    return null;
                }
            },

            // Rule 19: services remove requires --service-id
            new ValidationRule
            {
                Id = "SVC002",
                Description = "services remove requires --service-id",
                IsApplicable = args => args.Command?.ToLowerInvariant() == "services" &&
                                       args.Subcommand?.ToLowerInvariant() == "remove",
                Validate = args =>
                {
                    if (!args.HasOption("service-id"))
                        return "services remove requires --service-id <guid>";
                    return null;
                }
            },

            // Rule 20: GUID format validation for service-id
            new ValidationRule
            {
                Id = "VAL005",
                Description = "--service-id must be a valid GUID",
                IsApplicable = args => args.HasOption("service-id"),
                Validate = args =>
                {
                    var value = args.GetOption<string>("service-id");
                    Guid guid;
                    if (!Guid.TryParse(value, out guid))
                        return $"--service-id must be a valid GUID (got: '{value}')";
                    return null;
                }
            },

            // Rule 21: --criteria cannot be empty
            new ValidationRule
            {
                Id = "VAL006",
                Description = "--criteria cannot be empty",
                IsApplicable = args => args.HasOption("criteria"),
                Validate = args =>
                {
                    var value = args.GetOption<string>("criteria");
                    if (string.IsNullOrWhiteSpace(value))
                        return "--criteria cannot be empty";
                    return null;
                }
            }
        };

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a command definition by name.
        /// </summary>
        public static CommandDef GetCommand(string name)
        {
            return Commands.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                (c.Aliases != null && c.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase))));
        }

        /// <summary>
        /// Gets a global option definition by name.
        /// </summary>
        public static OptionDef GetGlobalOption(string name)
        {
            return GlobalOptions.FirstOrDefault(o =>
                o.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(o.ShortName) && o.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Gets a command-specific option definition.
        /// </summary>
        public static OptionDef GetCommandOption(string command, string optionName)
        {
            var cmd = GetCommand(command);
            if (cmd?.Options == null) return null;
            return cmd.Options.FirstOrDefault(o =>
                o.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all options available for a command (global + command-specific).
        /// </summary>
        public static IEnumerable<OptionDef> GetOptionsForCommand(string command)
        {
            var cmd = GetCommand(command);

            // Filter global options based on allowed/forbidden commands
            var globalOpts = GlobalOptions.Where(o =>
            {
                if (o.ForbiddenCommands != null && o.ForbiddenCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
                    return false;
                if (o.AllowedCommands != null && !o.AllowedCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
                    return false;
                return true;
            });

            if (cmd?.Options != null)
            {
                return globalOpts.Concat(cmd.Options);
            }
            return globalOpts;
        }

        #endregion
    }

    #endregion
}
