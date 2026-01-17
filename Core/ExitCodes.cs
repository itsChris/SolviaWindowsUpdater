namespace SolviaWindowsUpdater.Core
{
    /// <summary>
    /// Defines all exit codes for the application.
    /// </summary>
    public static class ExitCodes
    {
        /// <summary>Operation completed successfully.</summary>
        public const int Success = 0;

        /// <summary>Operation completed with partial success (some updates failed).</summary>
        public const int PartialSuccess = 1;

        /// <summary>Validation error (invalid arguments, missing required options).</summary>
        public const int ValidationError = 2;

        /// <summary>Operation requires administrator privileges.</summary>
        public const int ElevationRequired = 3;

        /// <summary>No updates found matching criteria.</summary>
        public const int NoUpdatesFound = 4;

        /// <summary>Windows Update API error.</summary>
        public const int WuaError = 5;

        /// <summary>EULA not accepted (required for install).</summary>
        public const int EulaNotAccepted = 6;

        /// <summary>Update not downloadable or not downloaded.</summary>
        public const int UpdateNotReady = 7;

        /// <summary>Update cannot be uninstalled.</summary>
        public const int UpdateNotUninstallable = 8;

        /// <summary>User cancelled the operation.</summary>
        public const int UserCancelled = 9;

        /// <summary>Unexpected error.</summary>
        public const int UnexpectedError = 10;

        /// <summary>
        /// Gets a human-readable description for an exit code.
        /// </summary>
        public static string GetDescription(int code)
        {
            switch (code)
            {
                case Success: return "Success";
                case PartialSuccess: return "Partial success (some operations failed)";
                case ValidationError: return "Validation error";
                case ElevationRequired: return "Administrator privileges required";
                case NoUpdatesFound: return "No updates found";
                case WuaError: return "Windows Update API error";
                case EulaNotAccepted: return "EULA not accepted";
                case UpdateNotReady: return "Update not ready (not downloaded)";
                case UpdateNotUninstallable: return "Update cannot be uninstalled";
                case UserCancelled: return "Operation cancelled by user";
                case UnexpectedError: return "Unexpected error";
                default: return "Unknown error";
            }
        }
    }
}
