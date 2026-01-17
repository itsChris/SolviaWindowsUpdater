using System.Security.Principal;

namespace SolviaWindowsUpdater.Core
{
    /// <summary>
    /// Helper class for checking administrator privileges.
    /// </summary>
    public static class AdminHelper
    {
        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Checks if the specified command requires administrator privileges.
        /// </summary>
        public static bool RequiresAdmin(string command)
        {
            switch (command?.ToLowerInvariant())
            {
                case "download":
                case "install":
                case "uninstall":
                    return true;
                case "services":
                    // services list doesn't require admin, but remove does
                    // This will be checked at a finer level
                    return false;
                case "search":
                case "history":
                case "status":
                case "help":
                case "version":
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the services subcommand requires admin.
        /// </summary>
        public static bool ServicesSubcommandRequiresAdmin(string subcommand)
        {
            switch (subcommand?.ToLowerInvariant())
            {
                case "remove":
                    return true;
                case "list":
                default:
                    return false;
            }
        }
    }
}
