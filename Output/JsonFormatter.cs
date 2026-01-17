using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Wua;

namespace SolviaWindowsUpdater.Output
{
    /// <summary>
    /// Formats data as JSON.
    /// </summary>
    public static class JsonFormatter
    {
        /// <summary>
        /// Formats a list of updates as JSON.
        /// </summary>
        public static string FormatUpdates(List<UpdateInfo> updates, OutputFormat format)
        {
            if (format == OutputFormat.JsonFull)
            {
                return FormatUpdatesFull(updates);
            }
            return FormatUpdatesMinimal(updates);
        }

        /// <summary>
        /// Formats a list of history entries as JSON.
        /// </summary>
        public static string FormatHistory(List<HistoryEntry> entries, OutputFormat format)
        {
            if (format == OutputFormat.JsonFull)
            {
                return FormatHistoryFull(entries);
            }
            return FormatHistoryMinimal(entries);
        }

        /// <summary>
        /// Formats a list of services as JSON.
        /// </summary>
        public static string FormatServices(List<ServiceInfo> services)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");

            for (int i = 0; i < services.Count; i++)
            {
                var s = services[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"serviceId\": {JsonString(s.ServiceId)},");
                sb.AppendLine($"    \"name\": {JsonString(s.Name)},");
                sb.AppendLine($"    \"type\": {JsonString(s.ServiceType)},");
                sb.AppendLine($"    \"isDefault\": {JsonBool(s.IsDefaultAUService)},");
                sb.AppendLine($"    \"isManaged\": {JsonBool(s.IsManaged)},");
                sb.AppendLine($"    \"offersWindowsUpdates\": {JsonBool(s.OffersWindowsUpdates)},");
                sb.AppendLine($"    \"serviceUrl\": {JsonString(s.ServiceUrl)}");
                sb.Append("  }");
                if (i < services.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("]");
            return sb.ToString();
        }

        /// <summary>
        /// Formats operation summary as JSON.
        /// </summary>
        public static string FormatOperationSummary(OperationSummary summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"totalCount\": {summary.TotalCount},");
            sb.AppendLine($"  \"successCount\": {summary.SuccessCount},");
            sb.AppendLine($"  \"failureCount\": {summary.FailureCount},");
            sb.AppendLine($"  \"rebootRequired\": {JsonBool(summary.RebootRequired)},");
            sb.AppendLine("  \"results\": [");

            for (int i = 0; i < summary.Results.Count; i++)
            {
                var r = summary.Results[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"kb\": {JsonString(r.Update.PrimaryKB)},");
                sb.AppendLine($"      \"title\": {JsonString(r.Update.Title)},");
                sb.AppendLine($"      \"success\": {JsonBool(r.Success)},");
                sb.AppendLine($"      \"resultCode\": {r.ResultCode},");
                sb.AppendLine($"      \"resultMessage\": {JsonString(r.ResultMessage)},");
                sb.AppendLine($"      \"hresult\": {r.HResult}");
                sb.Append("    }");
                if (i < summary.Results.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Formats system status as JSON.
        /// </summary>
        public static string FormatStatus(bool rebootRequired)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"rebootRequired\": {JsonBool(rebootRequired)}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #region Minimal Format

        private static string FormatUpdatesMinimal(List<UpdateInfo> updates)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");

            for (int i = 0; i < updates.Count; i++)
            {
                var u = updates[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"index\": {u.Index},");
                sb.AppendLine($"    \"kb\": {JsonString(u.PrimaryKB)},");
                sb.AppendLine($"    \"title\": {JsonString(u.Title)},");
                sb.AppendLine($"    \"size\": {u.MaxDownloadSize},");
                sb.AppendLine($"    \"severity\": {JsonString(u.MsrcSeverity)},");
                sb.AppendLine($"    \"status\": {JsonString(u.Status)}");
                sb.Append("  }");
                if (i < updates.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("]");
            return sb.ToString();
        }

        private static string FormatHistoryMinimal(List<HistoryEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"index\": {e.Index},");
                sb.AppendLine($"    \"date\": {JsonString(e.Date.ToString("o"))},");
                sb.AppendLine($"    \"operation\": {JsonString(e.Operation)},");
                sb.AppendLine($"    \"result\": {JsonString(e.ResultString)},");
                sb.AppendLine($"    \"title\": {JsonString(e.Title)}");
                sb.Append("  }");
                if (i < entries.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("]");
            return sb.ToString();
        }

        #endregion

        #region Full Format

        private static string FormatUpdatesFull(List<UpdateInfo> updates)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"count\": {updates.Count},");
            sb.AppendLine($"  \"timestamp\": {JsonString(DateTime.UtcNow.ToString("o"))},");
            sb.AppendLine("  \"updates\": [");

            for (int i = 0; i < updates.Count; i++)
            {
                var u = updates[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"index\": {u.Index},");
                sb.AppendLine($"      \"updateId\": {JsonString(u.UpdateId)},");
                sb.AppendLine($"      \"title\": {JsonString(u.Title)},");
                sb.AppendLine($"      \"description\": {JsonString(u.Description)},");
                sb.AppendLine($"      \"kbArticleIds\": {JsonArray(u.KBArticleIDs)},");
                sb.AppendLine($"      \"categories\": {JsonArray(u.Categories)},");
                sb.AppendLine($"      \"msrcSeverity\": {JsonString(u.MsrcSeverity)},");
                sb.AppendLine($"      \"maxDownloadSize\": {u.MaxDownloadSize},");
                sb.AppendLine($"      \"minDownloadSize\": {u.MinDownloadSize},");
                sb.AppendLine($"      \"isDownloaded\": {JsonBool(u.IsDownloaded)},");
                sb.AppendLine($"      \"isInstalled\": {JsonBool(u.IsInstalled)},");
                sb.AppendLine($"      \"isHidden\": {JsonBool(u.IsHidden)},");
                sb.AppendLine($"      \"isMandatory\": {JsonBool(u.IsMandatory)},");
                sb.AppendLine($"      \"isUninstallable\": {JsonBool(u.IsUninstallable)},");
                sb.AppendLine($"      \"isBeta\": {JsonBool(u.IsBeta)},");
                sb.AppendLine($"      \"autoSelectOnWebSites\": {JsonBool(u.AutoSelectOnWebSites)},");
                sb.AppendLine($"      \"eulaAccepted\": {JsonBool(u.EulaAccepted)},");
                sb.AppendLine($"      \"rebootBehavior\": {JsonString(u.RebootBehavior)},");
                sb.AppendLine($"      \"rebootRequired\": {JsonBool(u.RebootRequired)}");
                sb.Append("    }");
                if (i < updates.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string FormatHistoryFull(List<HistoryEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"count\": {entries.Count},");
            sb.AppendLine($"  \"timestamp\": {JsonString(DateTime.UtcNow.ToString("o"))},");
            sb.AppendLine("  \"history\": [");

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"index\": {e.Index},");
                sb.AppendLine($"      \"updateId\": {JsonString(e.UpdateId)},");
                sb.AppendLine($"      \"title\": {JsonString(e.Title)},");
                sb.AppendLine($"      \"description\": {JsonString(e.Description)},");
                sb.AppendLine($"      \"date\": {JsonString(e.Date.ToString("o"))},");
                sb.AppendLine($"      \"operation\": {JsonString(e.Operation)},");
                sb.AppendLine($"      \"resultCode\": {e.ResultCode},");
                sb.AppendLine($"      \"resultString\": {JsonString(e.ResultString)},");
                sb.AppendLine($"      \"hresult\": {e.HResult},");
                sb.AppendLine($"      \"clientApplicationId\": {JsonString(e.ClientApplicationId)},");
                sb.AppendLine($"      \"serviceId\": {JsonString(e.ServiceId)},");
                sb.AppendLine($"      \"supportUrl\": {JsonString(e.SupportUrl)}");
                sb.Append("    }");
                if (i < entries.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion

        #region JSON Helpers

        private static string JsonString(string value)
        {
            if (value == null) return "null";

            var sb = new StringBuilder();
            sb.Append('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string JsonArray(IEnumerable<string> values)
        {
            if (values == null) return "[]";
            var items = values.Select(v => JsonString(v));
            return "[" + string.Join(", ", items) + "]";
        }

        #endregion
    }
}
