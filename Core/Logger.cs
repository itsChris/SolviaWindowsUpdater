using System;
using System.IO;
using System.Text;

namespace SolviaWindowsUpdater.Core
{
    /// <summary>
    /// Log levels in order of verbosity.
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4
    }

    /// <summary>
    /// Centralized logging abstraction that writes to console and file.
    /// </summary>
    public sealed class Logger : IDisposable
    {
        private static Logger _instance;
        private static readonly object _lock = new object();

        private readonly string _logPath;
        private readonly LogLevel _minLevel;
        private StreamWriter _writer;
        private bool _disposed;

        /// <summary>
        /// Gets the singleton logger instance.
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("Logger not initialized. Call Initialize first.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initializes the logger with the specified settings.
        /// </summary>
        public static void Initialize(string logPath, LogLevel minLevel)
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = new Logger(logPath, minLevel);
            }
        }

        /// <summary>
        /// Checks if the logger has been initialized.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        private Logger(string logPath, LogLevel minLevel)
        {
            _logPath = logPath;
            _minLevel = minLevel;

            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _writer = new StreamWriter(logPath, true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Could not create log file '{logPath}': {ex.Message}");
                _writer = null;
            }
        }

        /// <summary>
        /// Logs a message at the specified level.
        /// </summary>
        public void Log(LogLevel level, string message)
        {
            if (level < _minLevel) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(5);
            var formattedMessage = $"[{timestamp}] [{levelStr}] {message}";

            // Write to file
            try
            {
                _writer?.WriteLine(formattedMessage);
            }
            catch
            {
                // Ignore file write errors
            }

            // Write to console with appropriate colors
            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                try
                {
                    switch (level)
                    {
                        case LogLevel.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case LogLevel.Warn:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case LogLevel.Info:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case LogLevel.Debug:
                        case LogLevel.Trace:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                    }

                    if (level >= LogLevel.Warn)
                    {
                        Console.Error.WriteLine(formattedMessage);
                    }
                    else
                    {
                        Console.WriteLine(formattedMessage);
                    }
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Logs a message at the specified level with format arguments.
        /// </summary>
        public void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Trace(string format, params object[] args) => Log(LogLevel.Trace, format, args);

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Debug(string format, params object[] args) => Log(LogLevel.Debug, format, args);

        public void Info(string message) => Log(LogLevel.Info, message);
        public void Info(string format, params object[] args) => Log(LogLevel.Info, format, args);

        public void Warn(string message) => Log(LogLevel.Warn, message);
        public void Warn(string format, params object[] args) => Log(LogLevel.Warn, format, args);

        public void Error(string message) => Log(LogLevel.Error, message);
        public void Error(string format, params object[] args) => Log(LogLevel.Error, format, args);

        public void Error(Exception ex, string message)
        {
            Log(LogLevel.Error, $"{message}: {ex.Message}");
            Log(LogLevel.Debug, $"Stack trace: {ex.StackTrace}");
        }

        /// <summary>
        /// Writes directly to console without timestamp (for output formatting).
        /// </summary>
        public void WriteOutput(string message)
        {
            Console.WriteLine(message);
            try
            {
                _writer?.WriteLine($"[OUTPUT] {message}");
            }
            catch
            {
                // Ignore file write errors
            }
        }

        /// <summary>
        /// Writes directly to console without newline (for progress updates).
        /// </summary>
        public void WriteProgress(string message)
        {
            Console.Write("\r" + message.PadRight(Console.WindowWidth - 1));
        }

        /// <summary>
        /// Clears the current progress line.
        /// </summary>
        public void ClearProgress()
        {
            try
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            }
            catch
            {
                Console.WriteLine();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _writer?.Dispose();
            _writer = null;
        }
    }
}
