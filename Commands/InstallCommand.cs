using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the install command.
    /// </summary>
    public static class InstallCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var criteria = args.GetOption<string>("criteria") ?? "IsInstalled=0";
            var selectAll = args.GetOption<bool>("all");
            var force = args.GetOption<bool>("force");
            var whatIf = args.GetOption<bool>("whatif");
            var acceptEulas = args.GetOption<bool>("accept-eulas");
            var noReboot = args.GetOption<bool>("noreboot");
            var serverMode = args.GetServerMode();
            var serviceId = args.GetOption<string>("service-id");

            // Parse selection
            string selectionType = null;
            string[] selectionValues = null;
            if (!selectAll)
            {
                if (!args.TryParseSelection(out selectionType, out selectionValues))
                {
                    Logger.Instance.Error("Invalid --select expression");
                    return ExitCodes.ValidationError;
                }
            }

            Logger.Instance.Info($"Searching for updates (criteria: {criteria})...");

            try
            {
                // Search for updates first
                var allUpdates = client.Search(criteria, serverMode, serviceId, false, 500);

                if (allUpdates.Count == 0)
                {
                    Logger.Instance.Info("No updates found matching the criteria.");
                    return ExitCodes.NoUpdatesFound;
                }

                // Apply selection
                var selectedUpdates = client.SelectUpdates(allUpdates, selectAll, selectionType, selectionValues);

                if (selectedUpdates.Count == 0)
                {
                    Logger.Instance.Warn("No updates matched the selection criteria.");
                    return ExitCodes.NoUpdatesFound;
                }

                Logger.Instance.Info($"Selected {selectedUpdates.Count} update(s) for installation.");

                // Check reboot status before
                bool rebootRequiredBefore = client.IsRebootRequired();
                if (rebootRequiredBefore)
                {
                    Logger.Instance.Warn("Note: A reboot is already pending from a previous operation.");
                }

                // Install
                var progress = new ProgressReporter(true);
                var summary = client.Install(selectedUpdates, force, acceptEulas, whatIf, progress);

                Console.WriteLine();
                Console.WriteLine(TableFormatter.FormatOperationResults(summary));

                // Handle reboot
                if (summary.RebootRequired && !whatIf)
                {
                    if (noReboot)
                    {
                        Logger.Instance.Warn("A system reboot is required to complete the installation.");
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
                            System.Diagnostics.Process.Start("shutdown", "/r /t 30 /c \"SolviaWindowsUpdater: Rebooting to complete update installation.\"");
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

                // Check for EULA failures
                foreach (var result in summary.Results)
                {
                    if (!result.Success && result.ResultMessage != null &&
                        result.ResultMessage.Contains("EULA"))
                    {
                        return ExitCodes.EulaNotAccepted;
                    }
                }

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Install failed");
                return ExitCodes.WuaError;
            }
        }
    }
}
