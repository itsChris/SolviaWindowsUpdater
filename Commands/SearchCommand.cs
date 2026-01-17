using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the search command.
    /// </summary>
    public static class SearchCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var criteria = args.GetOption<string>("criteria") ?? "IsInstalled=0";
            var includeHidden = args.GetOption<bool>("include-hidden");
            var maxResults = args.GetOption<int>("max-results");
            if (maxResults <= 0) maxResults = 50;

            var serverMode = args.GetServerMode();
            var serviceId = args.GetOption<string>("service-id");
            var outputFormat = args.GetOutputFormat();

            Logger.Instance.Info($"Searching for updates (criteria: {criteria})...");

            try
            {
                var updates = client.Search(criteria, serverMode, serviceId, includeHidden, maxResults);

                if (updates.Count == 0)
                {
                    Logger.Instance.Info("No updates found matching the criteria.");
                    return ExitCodes.NoUpdatesFound;
                }

                Logger.Instance.Info($"Found {updates.Count} update(s).");
                Console.WriteLine();

                string output;
                switch (outputFormat)
                {
                    case OutputFormat.Json:
                    case OutputFormat.JsonFull:
                        output = JsonFormatter.FormatUpdates(updates, outputFormat);
                        break;
                    default:
                        output = TableFormatter.FormatUpdates(updates);
                        break;
                }

                Console.WriteLine(output);
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Search failed");
                return ExitCodes.WuaError;
            }
        }
    }
}
