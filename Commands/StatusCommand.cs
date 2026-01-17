using System;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Commands
{
    /// <summary>
    /// Handles the status command.
    /// </summary>
    public static class StatusCommand
    {
        public static int Execute(ParsedArgs args, WuaClient client)
        {
            Logger.Instance.Info("Checking system status...");

            try
            {
                bool rebootRequired = client.IsRebootRequired();

                Console.WriteLine();
                Console.WriteLine("System Status:");
                Console.WriteLine("==============");
                Console.WriteLine();

                if (rebootRequired)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  Reboot Required: YES");
                    Console.WriteLine();
                    Console.WriteLine("  A system reboot is pending to complete previous update operations.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  Reboot Required: NO");
                    Console.ResetColor();
                }

                Console.WriteLine();

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, "Failed to check status");
                return ExitCodes.WuaError;
            }
        }
    }
}
