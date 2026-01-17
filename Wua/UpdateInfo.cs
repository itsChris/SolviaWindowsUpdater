using System;
using System.Collections.Generic;

namespace SolviaWindowsUpdater.Wua
{
    /// <summary>
    /// Represents information about a Windows Update.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// Index in the search results (1-based for user display).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Update title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Update description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// KB article IDs associated with this update.
        /// </summary>
        public List<string> KBArticleIDs { get; set; } = new List<string>();

        /// <summary>
        /// Gets the primary KB ID or empty string.
        /// </summary>
        public string PrimaryKB
        {
            get { return KBArticleIDs.Count > 0 ? KBArticleIDs[0] : ""; }
        }

        /// <summary>
        /// Categories this update belongs to.
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// MSRC severity (Critical, Important, Moderate, Low, Unspecified).
        /// </summary>
        public string MsrcSeverity { get; set; }

        /// <summary>
        /// Maximum download size in bytes.
        /// </summary>
        public long MaxDownloadSize { get; set; }

        /// <summary>
        /// Minimum download size in bytes.
        /// </summary>
        public long MinDownloadSize { get; set; }

        /// <summary>
        /// Whether the update is downloaded.
        /// </summary>
        public bool IsDownloaded { get; set; }

        /// <summary>
        /// Whether the update is installed.
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Whether the update is hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Whether the update is mandatory.
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// Whether the update can be uninstalled.
        /// </summary>
        public bool IsUninstallable { get; set; }

        /// <summary>
        /// Whether the update is a beta release.
        /// </summary>
        public bool IsBeta { get; set; }

        /// <summary>
        /// Whether the update auto-selects on web sites.
        /// </summary>
        public bool AutoSelectOnWebSites { get; set; }

        /// <summary>
        /// Whether the EULA has been accepted.
        /// </summary>
        public bool EulaAccepted { get; set; }

        /// <summary>
        /// Reboot behavior after installation.
        /// </summary>
        public string RebootBehavior { get; set; }

        /// <summary>
        /// Whether a reboot is required after installation.
        /// </summary>
        public bool RebootRequired { get; set; }

        /// <summary>
        /// The update identity (for internal use).
        /// </summary>
        public string UpdateId { get; set; }

        /// <summary>
        /// Reference to the underlying IUpdate COM object.
        /// </summary>
        internal object ComObject { get; set; }

        /// <summary>
        /// Gets a formatted size string.
        /// </summary>
        public string FormattedSize
        {
            get { return FormatBytes(MaxDownloadSize); }
        }

        /// <summary>
        /// Gets the status string for display.
        /// </summary>
        public string Status
        {
            get
            {
                if (IsInstalled) return "Installed";
                if (IsDownloaded) return "Downloaded";
                if (IsHidden) return "Hidden";
                return "Available";
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "Unknown";
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Result of an update operation (download/install/uninstall).
    /// </summary>
    public class UpdateOperationResult
    {
        public UpdateInfo Update { get; set; }
        public bool Success { get; set; }
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; }
        public int HResult { get; set; }
    }

    /// <summary>
    /// Summary of a batch operation.
    /// </summary>
    public class OperationSummary
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public bool RebootRequired { get; set; }
        public List<UpdateOperationResult> Results { get; set; } = new List<UpdateOperationResult>();

        public bool AllSucceeded => FailureCount == 0 && SuccessCount > 0;
        public bool AllFailed => SuccessCount == 0 && FailureCount > 0;
        public bool PartialSuccess => SuccessCount > 0 && FailureCount > 0;
    }
}
