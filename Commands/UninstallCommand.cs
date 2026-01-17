using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the uninstall command.
    /// </summary>
    public static class UninstallCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var force = args.GetOption<bool>("force");
            var whatIf = args.GetOption<bool>("whatif");
            var noReboot = args.GetOption<bool>("noreboot");
            var serverMode = args.GetServerMode();
            var serviceId = args.GetOption<string>("service-id");

            // Parse selection (required for uninstall)
            string selectionType;
            string[] selectionValues;
            if (!args.TryParseSelection(out selectionType, out selectionValues))
            {
                Logger.Instance.Error("Invalid --select expression");
                return ExitCodes.ValidationError;
            }

            // For uninstall, we need to search for installed updates
            var criteria = "IsInstalled=1";
            Logger.Instance.Info($"Searching for installed updates...");

            try
            {
                // Search for installed updates
                var allUpdates = client.Search(criteria, serverMode, serviceId, false, 500);

                if (allUpdates.Count == 0)
                {
                    Logger.Instance.Info("No installed updates found.");
                    return ExitCodes.NoUpdatesFound;
                }

                // Apply selection
                var selectedUpdates = client.SelectUpdates(allUpdates, false, selectionType, selectionValues);

                if (selectedUpdates.Count == 0)
                {
                    Logger.Instance.Warn("No installed updates matched the selection criteria.");
                    return ExitCodes.NoUpdatesFound;
                }

                Logger.Instance.Info($"Selected {selectedUpdates.Count} update(s) for uninstallation.");

                // Uninstall
                var progress = new ProgressReporter(true);
                var summary = client.Uninstall(selectedUpdates, force, whatIf, progress);

                Console.WriteLine();
                Console.WriteLine(TableFormatter.FormatOperationResults(summary));

                // Handle reboot
                if (summary.RebootRequired && !whatIf)
                {
                    if (noReboot)
                    {
                        Logger.Instance.Warn("A system reboot is required to complete the uninstallation.");
                        Logger.Instance.Info("Reboot suppressed (--noreboot). Please reboot manually when ready.");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.Write("A system reboot is required. Reboot now? [Y/N]: ");
                        var response = Console.ReadLine();
                        if (response != null && response.Trim().ToUpperInvariant().StartsWith("Y"))
                        {
                            Logger.Instance.Info("Initiating system reboot...");
                            System.Diagnostics.Process.Start("shutdown", "/r /t 30 /c \"SolviaWindowsUpdater: Rebooting to complete update uninstallation.\"");
                            Logger.Instance.Info("System will reboot in 30 seconds. Use 'shutdown /a' to abort.");
                        }
                        else
                        {
                            Logger.Instance.Info("Reboot cancelled. Please reboot manually when ready.");
                        }
                    }
                }

                if (summary.AllFailed)
                    return ExitCodes.WuaError;
                if (summary.PartialSuccess)
                    return ExitCodes.PartialSuccess;

                // Check for uninstallable failures
                foreach (var result in summary.Results)
                {
                    if (!result.Success && result.ResultMessage != null &&
                        result.ResultMessage.Contains("cannot be uninstalled"))
                    {
                        return ExitCodes.UpdateNotUninstallable;
                    }
                }

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Uninstall failed");
                return ExitCodes.WuaError;
            }
        }
    }
}
