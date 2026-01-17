using System;
using SolviaWindowsUpdater.Core;

namespace SolviaWindowsUpdater.Wua
{
    /// <summary>
    /// Reports progress for update operations.
    /// </summary>
    public class ProgressReporter
    {
        private readonly bool _enabled;
        private int _currentIndex;
        private int _totalCount;
        private string _currentTitle;
        private int _lastPercentage;

        public ProgressReporter(bool enabled = true)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Starts a new batch operation.
        /// </summary>
        public void StartBatch(string operation, int totalCount)
        {
            _totalCount = totalCount;
            _currentIndex = 0;
            _lastPercentage = -1;

            if (_enabled && Logger.IsInitialized)
            {
                Logger.Instance.Info($"Starting {operation} of {totalCount} update(s)...");
            }
        }

        /// <summary>
        /// Starts processing a specific update.
        /// </summary>
        public void StartUpdate(int index, string title, string operation)
        {
            _currentIndex = index;
            _currentTitle = TruncateTitle(title, 50);
            _lastPercentage = -1;

            if (_enabled)
            {
                WriteProgress(0, operation);
            }
        }

        /// <summary>
        /// Reports progress percentage for current update.
        /// </summary>
        public void ReportProgress(int percentComplete, string operation)
        {
            if (!_enabled) return;

            // Only update if percentage changed significantly
            if (percentComplete == _lastPercentage) return;
            _lastPercentage = percentComplete;

            WriteProgress(percentComplete, operation);
        }

        /// <summary>
        /// Completes the current update.
        /// </summary>
        public void CompleteUpdate(bool success, string message = null)
        {
            if (_enabled && Logger.IsInitialized)
            {
                Logger.Instance.ClearProgress();
                var status = success ? "Done" : "Failed";
                var fullMessage = $"[{_currentIndex}/{_totalCount}] {_currentTitle} - {status}";
                if (!string.IsNullOrEmpty(message))
                {
                    fullMessage += $": {message}";
                }
                Logger.Instance.WriteOutput(fullMessage);
            }
        }

        /// <summary>
        /// Completes the entire batch operation.
        /// </summary>
        public void CompleteBatch(OperationSummary summary)
        {
            if (_enabled && Logger.IsInitialized)
            {
                Logger.Instance.ClearProgress();
                Console.WriteLine();
                Logger.Instance.Info($"Completed: {summary.SuccessCount} succeeded, {summary.FailureCount} failed");
                if (summary.RebootRequired)
                {
                    Logger.Instance.Warn("A system reboot is required to complete the installation.");
                }
            }
        }

        private void WriteProgress(int percentComplete, string operation)
        {
            if (!Logger.IsInitialized) return;

            var progressBar = BuildProgressBar(percentComplete, 20);
            var message = $"[{_currentIndex}/{_totalCount}] {progressBar} {percentComplete,3}% - {operation} {_currentTitle}";
            Logger.Instance.WriteProgress(message);
        }

        private static string BuildProgressBar(int percent, int width)
        {
            var filled = (int)((percent / 100.0) * width);
            var empty = width - filled;
            return "[" + new string('#', filled) + new string('-', empty) + "]";
        }

        private static string TruncateTitle(string title, int maxLength)
        {
            if (string.IsNullOrEmpty(title)) return "";
            if (title.Length <= maxLength) return title;
            return title.Substring(0, maxLength - 3) + "...";
        }
    }
}
