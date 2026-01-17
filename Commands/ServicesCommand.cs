using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Output;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the services command.
    /// </summary>
    public static class ServicesCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            var subcommand = args.Subcommand?.ToLowerInvariant();

            switch (subcommand)
            {
                case "list":
                    return ExecuteList(client);
                case "remove":
                    return ExecuteRemove(args, client);
                default:
                    Logger.Instance.Error($"Unknown services subcommand: {subcommand}");
                    return ExitCodes.ValidationError;
            }
        }

        private static int ExecuteList(WuaClient client)
        {
            Logger.Instance.Info("Listing registered update services...");

            try
            {
                var services = client.GetServices();

                if (services.Count == 0)
                {
                    Logger.Instance.Info("No services found.");
                    return ExitCodes.Success;
                }

                Logger.Instance.Info($"Found {services.Count} service(s).");
                Console.WriteLine();
                Console.WriteLine(TableFormatter.FormatServices(services));

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to list services");
                return ExitCodes.WuaError;
            }
        }

        private static int ExecuteRemove(ParsedArgs args, WuaClient client)
        {
            var serviceId = args.GetOption<string>("service-id");

            if (string.IsNullOrEmpty(serviceId))
            {
                Logger.Instance.Error("--service-id is required for services remove");
                return ExitCodes.ValidationError;
            }

            // Validate GUID format
            Guid guid;
            if (!Guid.TryParse(serviceId, out guid))
            {
                Logger.Instance.Error($"Invalid service ID format: {serviceId}");
                return ExitCodes.ValidationError;
            }

            Logger.Instance.Info($"Removing service: {serviceId}...");

            try
            {
                client.RemoveService(serviceId);
                Logger.Instance.Info("Service removed successfully.");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to remove service");
                return ExitCodes.WuaError;
            }
        }
    }
}
