using System;

namespace SolviaWindowsUpdater.Wua
{
    /// <summary>
    /// Represents an entry in the Windows Update history.
    /// </summary>
    public class HistoryEntry
    {
        /// <summary>
        /// Index in the history results.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Operation type (Install or Uninstall).
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Result code of the operation.
        /// </summary>
        public int ResultCode { get; set; }

        /// <summary>
        /// Result code as human-readable string.
        /// </summary>
        public string ResultString
        {
            get { return GetResultString(ResultCode); }
        }

        /// <summary>
        /// Date and time of the operation.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Title of the update.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description of the update.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Update identity.
        /// </summary>
        public string UpdateId { get; set; }

        /// <summary>
        /// Client application that initiated the update.
        /// </summary>
        public string ClientApplicationId { get; set; }

        /// <summary>
        /// Server selection used for the update.
        /// </summary>
        public string ServerSelection { get; set; }

        /// <summary>
        /// Service ID used.
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// Support URL for the update.
        /// </summary>
        public string SupportUrl { get; set; }

        /// <summary>
        /// HRESULT if the operation failed.
        /// </summary>
        public int HResult { get; set; }

        /// <summary>
        /// Categories of the update.
        /// </summary>
        public string[] Categories { get; set; }

        private static string GetResultString(int resultCode)
        {
            switch (resultCode)
            {
                case 0: return "Not Started";
                case 1: return "In Progress";
                case 2: return "Succeeded";
                case 3: return "Succeeded with Errors";
                case 4: return "Failed";
                case 5: return "Aborted";
                default: return $"Unknown ({resultCode})";
            }
        }
    }
}
