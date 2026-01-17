using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Output
{
    /// <summary>
    /// Formats data as console tables.
    /// </summary>
    public static class TableFormatter
    {
        /// <summary>
        /// Formats a list of updates as a table.
        /// </summary>
        public static string FormatUpdates(List<UpdateInfo> updates)
        {
            if (updates == null || updates.Count == 0)
            {
                return "No updates found.";
            }

            var columns = new[]
            {
                new Column { Header = "#", Width = 4, Align = Alignment.Right },
                new Column { Header = "KB", Width = 12 },
                new Column { Header = "Title", Width = 0 },  // Auto-size
                new Column { Header = "Size", Width = 10, Align = Alignment.Right },
                new Column { Header = "Severity", Width = 12 },
                new Column { Header = "Status", Width = 12 }
            };

            var rows = updates.Select(u => new[]
            {
                u.Index.ToString(),
                u.PrimaryKB,
                u.Title ?? "",
                u.FormattedSize,
                u.MsrcSeverity ?? "Unspecified",
                u.Status
            }).ToList();

            return FormatTable(columns, rows);
        }

        /// <summary>
        /// Formats a list of history entries as a table.
        /// </summary>
        public static string FormatHistory(List<HistoryEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "No history entries found.";
            }

            var columns = new[]
            {
                new Column { Header = "#", Width = 5, Align = Alignment.Right },
                new Column { Header = "Date", Width = 20 },
                new Column { Header = "Operation", Width = 12 },
                new Column { Header = "Result", Width = 20 },
                new Column { Header = "Title", Width = 0 }  // Auto-size
            };

            var rows = entries.Select(e => new[]
            {
                e.Index.ToString(),
                e.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                e.Operation,
                e.ResultString,
                e.Title ?? ""
            }).ToList();

            return FormatTable(columns, rows);
        }

        /// <summary>
        /// Formats a list of services as a table.
        /// </summary>
        public static string FormatServices(List<ServiceInfo> services)
        {
            if (services == null || services.Count == 0)
            {
                return "No services found.";
            }

            var columns = new[]
            {
                new Column { Header = "Service ID", Width = 38 },
                new Column { Header = "Name", Width = 0 },  // Auto-size
                new Column { Header = "Type", Width = 14 },
                new Column { Header = "Default", Width = 8 }
            };

            var rows = services.Select(s => new[]
            {
                s.ServiceId ?? "",
                s.Name ?? "",
                s.ServiceType,
                s.IsDefaultAUService ? "Yes" : "No"
            }).ToList();

            return FormatTable(columns, rows);
        }

        /// <summary>
        /// Formats operation results as a table.
        /// </summary>
        public static string FormatOperationResults(OperationSummary summary)
        {
            var sb = new StringBuilder();

            if (summary.Results.Count > 0)
            {
                var columns = new[]
                {
                    new Column { Header = "KB", Width = 12 },
                    new Column { Header = "Title", Width = 0 },
                    new Column { Header = "Result", Width = 25 }
                };

                var rows = summary.Results.Select(r => new[]
                {
                    r.Update.PrimaryKB,
                    r.Update.Title ?? "",
                    r.Success ? "Success" : $"Failed: {r.ResultMessage}"
                }).ToList();

                sb.AppendLine(FormatTable(columns, rows));
            }

            sb.AppendLine();
            sb.AppendLine($"Total: {summary.TotalCount}, Succeeded: {summary.SuccessCount}, Failed: {summary.FailureCount}");

            if (summary.RebootRequired)
            {
                sb.AppendLine();
                sb.AppendLine("*** A system reboot is required to complete the operation. ***");
            }

            return sb.ToString();
        }

        private static string FormatTable(Column[] columns, List<string[]> rows)
        {
            // Calculate column widths
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Width == 0)  // Auto-size
                {
                    int maxWidth = columns[i].Header.Length;
                    foreach (var row in rows)
                    {
                        if (i < row.Length && row[i] != null)
                        {
                            maxWidth = Math.Max(maxWidth, row[i].Length);
                        }
                    }
                    // Cap auto-sized columns at reasonable width
                    columns[i].Width = Math.Min(maxWidth, GetMaxAutoWidth());
                }
            }

            var sb = new StringBuilder();

            // Header row
            sb.Append(FormatRow(columns, columns.Select(c => c.Header).ToArray()));
            sb.AppendLine();

            // Separator
            sb.Append(FormatSeparator(columns));
            sb.AppendLine();

            // Data rows
            foreach (var row in rows)
            {
                sb.Append(FormatRow(columns, row));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string FormatRow(Column[] columns, string[] values)
        {
            var parts = new List<string>();

            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                var value = i < values.Length ? (values[i] ?? "") : "";

                // Truncate if necessary
                if (value.Length > col.Width)
                {
                    value = value.Substring(0, col.Width - 3) + "...";
                }

                // Align
                switch (col.Align)
                {
                    case Alignment.Right:
                        value = value.PadLeft(col.Width);
                        break;
                    case Alignment.Center:
                        var padding = (col.Width - value.Length) / 2;
                        value = value.PadLeft(value.Length + padding).PadRight(col.Width);
                        break;
                    default:
                        value = value.PadRight(col.Width);
                        break;
                }

                parts.Add(value);
            }

            return string.Join(" | ", parts);
        }

        private static string FormatSeparator(Column[] columns)
        {
            var parts = columns.Select(c => new string('-', c.Width));
            return string.Join("-+-", parts);
        }

        private static int GetMaxAutoWidth()
        {
            try
            {
                return Math.Max(40, Console.WindowWidth - 60);
            }
            catch
            {
                return 60;
            }
        }

        private class Column
        {
            public string Header { get; set; }
            public int Width { get; set; }
            public Alignment Align { get; set; } = Alignment.Left;
        }

        private enum Alignment
        {
            Left,
            Right,
            Center
        }
    }
}
