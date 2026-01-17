using System;
using System.Runtime.InteropServices;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Commands;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater
{
    /// <summary>
    /// Main entry point for SolviaWindowsUpdater CLI.
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            int exitCode = ExitCodes.Success;

            try
            {
                exitCode = Run(args);
            }
            catch (Exception ex)
            {
                // Last resort error handling
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();

                if (Logger.IsInitialized)
                {
                    Logger.Instance.Error(ex, "Fatal error");
                }

                exitCode = ExitCodes.UnexpectedError;
            }
            finally
            {
                if (Logger.IsInitialized)
                {
                    Logger.Instance.Debug($"Exiting with code {exitCode} ({ExitCodes.GetDescription(exitCode)})");
                    Logger.Instance.Dispose();
                }
            }

            return exitCode;
        }

        private static int Run(string[] args)
        {
            // Parse arguments
            var parsedArgs = ArgParser.Parse(args);

            // Handle help and version early (before logger initialization)
            if (parsedArgs.Command == "help" || parsedArgs.GetOption<bool>("help"))
            {
                return HandleHelp(parsedArgs);
            }

            if (parsedArgs.Command == "version" || parsedArgs.GetOption<bool>("version"))
            {
                Console.WriteLine(HelpGenerator.GenerateVersion());
                return ExitCodes.Success;
            }

            // Initialize logger
            var logPath = parsedArgs.GetOption<string>("log-path") ?? CliSpec.DefaultLogPath;
            logPath = Environment.ExpandEnvironmentVariables(logPath);

            var logLevelStr = parsedArgs.GetOption<string>("log-level") ?? "Info";
            LogLevel logLevel;
            if (!Enum.TryParse(logLevelStr, true, out logLevel))
            {
                logLevel = LogLevel.Info;
            }

            Logger.Initialize(logPath, logLevel);
            Logger.Instance.Debug($"SolviaWindowsUpdater v{CliSpec.Version} starting");
            Logger.Instance.Debug($"Command line: {string.Join(" ", args)}");

            // Validate arguments
            var validation = Validator.ValidateFull(parsedArgs);
            if (!validation.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(validation.GetFormattedErrors());
                Console.ResetColor();
                return ExitCodes.ValidationError;
            }

            // Check admin requirements
            var command = parsedArgs.Command?.ToLowerInvariant();
            bool requiresAdmin = AdminHelper.RequiresAdmin(command);

            // Special check for services subcommand
            if (command == "services" && parsedArgs.Subcommand != null)
            {
                requiresAdmin = AdminHelper.ServicesSubcommandRequiresAdmin(parsedArgs.Subcommand);
            }

            if (requiresAdmin && !AdminHelper.IsRunningAsAdmin())
            {
                Logger.Instance.Error("This operation requires administrator privileges.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: This operation requires administrator privileges.");
                Console.Error.WriteLine("Please run this command from an elevated command prompt.");
                Console.ResetColor();
                return ExitCodes.ElevationRequired;
            }

            // Create WUA client and execute command
            using (var client = CreateClient())
            {
                if (client == null)
                {
                    return ExitCodes.WuaError;
                }

                return ExecuteCommand(parsedArgs, client);
            }
        }

        private static int HandleHelp(ParsedArgs args)
        {
            // Check if help is for a specific command
            if (args.PositionalArgs.Count > 0)
            {
                var cmdName = args.PositionalArgs[0];
                Console.WriteLine(HelpGenerator.GenerateCommandHelp(cmdName));
            }
            else if (!string.IsNullOrEmpty(args.Command) && args.Command != "help")
            {
                Console.WriteLine(HelpGenerator.GenerateCommandHelp(args.Command));
            }
            else
            {
                Console.WriteLine(HelpGenerator.GenerateFullHelp());
            }
            return ExitCodes.Success;
        }

        private static WuaClient CreateClient()
        {
            try
            {
                return new WuaClient();
            }
            catch (COMException ex)
            {
                Logger.Instance.Error($"Failed to initialize Windows Update API: 0x{ex.ErrorCode:X8}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: Failed to initialize Windows Update API.");
                Console.Error.WriteLine("Ensure the Windows Update service is running.");
                Console.ResetColor();
                return null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to create WUA client");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        private static int ExecuteCommand(ParsedArgs args, WuaClient client)
        {
            var command = args.Command?.ToLowerInvariant();

            Logger.Instance.Debug($"Executing command: {command}");

            switch (command)
            {
                case "search":
                    return SearchCommand.Execute(args, client);

                case "download":
                    return DownloadCommand.Execute(args, client);

                case "install":
                    return InstallCommand.Execute(args, client);

                case "uninstall":
                    return UninstallCommand.Execute(args, client);

                case "history":
                    return HistoryCommand.Execute(args, client);

                case "services":
                    return ServicesCommand.Execute(args, client);

                case "status":
                    return StatusCommand.Execute(args, client);

                default:
                    Logger.Instance.Error($"Unknown command: {command}");
                    return ExitCodes.ValidationError;
            }
        }
    }
}
