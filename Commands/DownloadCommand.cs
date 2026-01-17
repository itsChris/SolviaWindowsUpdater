using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the download command.
    /// </summary>
    public static class DownloadCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var criteria = args.GetOption<string>("criteria") ?? "IsInstalled=0";
            var selectAll = args.GetOption<bool>("all");
            var force = args.GetOption<bool>("force");
            var whatIf = args.GetOption<bool>("whatif");
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

                Logger.Instance.Info($"Selected {selectedUpdates.Count} update(s) for download.");

                // Download
                var progress = new ProgressReporter(true);
                var summary = client.Download(selectedUpdates, force, whatIf, progress);

                Console.WriteLine();
                Console.WriteLine(TableFormatter.FormatOperationResults(summary));

                if (summary.AllFailed)
                    return ExitCodes.WuaError;
                if (summary.PartialSuccess)
                    return ExitCodes.PartialSuccess;

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Download failed");
                return ExitCodes.WuaError;
            }
        }
    }
}
