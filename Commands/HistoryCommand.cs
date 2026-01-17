using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the history command.
    /// </summary>
    public static class HistoryCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var startIndex = args.GetOption<int>("start-index");
            var count = args.GetOption<int>("count");
            var outputFormat = args.GetOutputFormat();

            // If count not specified (0), get all history entries
            if (count <= 0)
            {
                count = client.GetHistoryCount();
                if (count <= 0)
                {
                    Logger.Instance.Info("No history entries found.");
                    return ExitCodes.Success;
                }
            }

            Logger.Instance.Info($"Querying update history (start: {startIndex}, count: {count})...");

            try
            {
                var history = client.GetHistory(startIndex, count);

                if (history.Count == 0)
                {
                    Logger.Instance.Info("No history entries found.");
                    return ExitCodes.Success;
                }

                Logger.Instance.Info($"Retrieved {history.Count} history entries.");
                Console.WriteLine();

                string output;
                switch (outputFormat)
                {
                    case OutputFormat.Json:
                    case OutputFormat.JsonFull:
                        output = JsonFormatter.FormatHistory(history, outputFormat);
                        break;
                    default:
                        output = TableFormatter.FormatHistory(history);
                        break;
                }

                Console.WriteLine(output);
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to retrieve history");
                return ExitCodes.WuaError;
            }
        }
    }
}
