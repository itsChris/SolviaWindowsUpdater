using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SolviaWindowsUpdater.Cli;
using SolviaWindowsUpdater.Core;
using WUApiLib;

namespace SolviaWindowsUpdater.Wua
{
    /// <summary>
    /// Windows Update Agent client wrapper.
    /// </summary>
    public class WuaClient : IDisposable
    {
        private IUpdateSession3 _session;
        private IUpdateSearcher _searcher;
        private IUpdateServiceManager2 _serviceManager;
        private ISystemInformation _systemInfo;
        private bool _disposed;

        public WuaClient()
        {
            try
            {
                _session = new UpdateSession() as IUpdateSession3;
                if (_session == null)
                {
                    throw new InvalidOperationException("Failed to create Windows Update session. Ensure Windows Update service is running.");
                }

                _searcher = _session.CreateUpdateSearcher();
                _serviceManager = new UpdateServiceManager() as IUpdateServiceManager2;
                _systemInfo = new SystemInformation() as ISystemInformation;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Failed to initialize Windows Update API: {GetComErrorMessage(ex)}", ex);
            }
        }

        #region Search Operations

        /// <summary>
        /// Searches for updates matching the criteria.
        /// </summary>
        public List<UpdateInfo> Search(string criteria, ServerMode serverMode, string serviceId, bool includeHidden, int maxResults)
        {
            if (Logger.IsInitialized)
                Logger.Instance.Debug($"Searching for updates with criteria: {criteria}");

            ConfigureSearcher(serverMode, serviceId);

            // Modify criteria to include/exclude hidden updates
            if (!includeHidden && !criteria.ToLowerInvariant().Contains("ishidden"))
            {
                criteria = $"({criteria}) AND IsHidden=0";
            }

            try
            {
                var searchResult = _searcher.Search(criteria);
                var updates = new List<UpdateInfo>();

                int count = Math.Min(searchResult.Updates.Count, maxResults);
                for (int i = 0; i < count; i++)
                {
                    var update = searchResult.Updates[i];
                    updates.Add(ConvertToUpdateInfo(update, i + 1));
                }

                if (Logger.IsInitialized)
                    Logger.Instance.Debug($"Found {updates.Count} updates");

                return updates;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Search failed: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region Download Operations

        /// <summary>
        /// Downloads the specified updates.
        /// </summary>
        public OperationSummary Download(List<UpdateInfo> updates, bool force, bool whatIf, ProgressReporter progress)
        {
            var summary = new OperationSummary { TotalCount = updates.Count };

            if (whatIf)
            {
                if (Logger.IsInitialized)
                    Logger.Instance.Info("WhatIf mode: The following updates would be downloaded:");

                foreach (var update in updates)
                {
                    if (Logger.IsInitialized)
                        Logger.Instance.WriteOutput($"  - {update.PrimaryKB}: {update.Title} ({update.FormattedSize})");

                    summary.Results.Add(new UpdateOperationResult
                    {
                        Update = update,
                        Success = true,
                        ResultMessage = "WhatIf: Would download"
                    });
                    summary.SuccessCount++;
                }
                return summary;
            }

            try
            {
                var downloader = _session.CreateUpdateDownloader();
                var updateCollection = CreateUpdateCollection();

                foreach (var update in updates)
                {
                    if (!update.IsDownloaded || force)
                    {
                        updateCollection.Add((IUpdate)update.ComObject);
                    }
                }

                if (updateCollection.Count == 0)
                {
                    if (Logger.IsInitialized)
                        Logger.Instance.Info("All selected updates are already downloaded.");

                    foreach (var update in updates)
                    {
                        summary.Results.Add(new UpdateOperationResult
                        {
                            Update = update,
                            Success = true,
                            ResultMessage = "Already downloaded"
                        });
                        summary.SuccessCount++;
                    }
                    return summary;
                }

                downloader.Updates = updateCollection;
                downloader.IsForced = force;

                progress?.StartBatch("download", updateCollection.Count);

                // Download all at once
                var result = downloader.Download();

                // Process results
                for (int i = 0; i < updateCollection.Count; i++)
                {
                    var update = updates.FirstOrDefault(u => u.ComObject == updateCollection[i]);
                    if (update == null) continue;

                    var updateResult = result.GetUpdateResult(i);
                    var success = updateResult.ResultCode == OperationResultCode.orcSucceeded ||
                                  updateResult.ResultCode == OperationResultCode.orcSucceededWithErrors;

                    var opResult = new UpdateOperationResult
                    {
                        Update = update,
                        Success = success,
                        ResultCode = (int)updateResult.ResultCode,
                        ResultMessage = GetResultCodeString(updateResult.ResultCode),
                        HResult = updateResult.HResult
                    };

                    summary.Results.Add(opResult);
                    if (success)
                        summary.SuccessCount++;
                    else
                        summary.FailureCount++;

                    progress?.CompleteUpdate(success, opResult.ResultMessage);
                }

                progress?.CompleteBatch(summary);
                return summary;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Download failed: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region Install Operations

        /// <summary>
        /// Installs the specified updates.
        /// </summary>
        public OperationSummary Install(List<UpdateInfo> updates, bool force, bool acceptEulas, bool whatIf, ProgressReporter progress)
        {
            var summary = new OperationSummary { TotalCount = updates.Count };

            // Check EULA status
            foreach (var update in updates)
            {
                var iUpdate = (IUpdate)update.ComObject;
                if (!iUpdate.EulaAccepted)
                {
                    if (acceptEulas)
                    {
                        try
                        {
                            iUpdate.AcceptEula();
                            update.EulaAccepted = true;
                        }
                        catch (COMException ex)
                        {
                            if (Logger.IsInitialized)
                                Logger.Instance.Warn($"Failed to accept EULA for {update.PrimaryKB}: {GetComErrorMessage(ex)}");
                        }
                    }
                    else
                    {
                        summary.Results.Add(new UpdateOperationResult
                        {
                            Update = update,
                            Success = false,
                            ResultMessage = "EULA not accepted. Use --accept-eulas to accept."
                        });
                        summary.FailureCount++;
                    }
                }
            }

            // Filter out updates that failed EULA check
            var installable = updates.Where(u => !summary.Results.Any(r => r.Update == u)).ToList();

            if (whatIf)
            {
                if (Logger.IsInitialized)
                    Logger.Instance.Info("WhatIf mode: The following updates would be installed:");

                foreach (var update in installable)
                {
                    if (Logger.IsInitialized)
                        Logger.Instance.WriteOutput($"  - {update.PrimaryKB}: {update.Title}");

                    summary.Results.Add(new UpdateOperationResult
                    {
                        Update = update,
                        Success = true,
                        ResultMessage = "WhatIf: Would install"
                    });
                    summary.SuccessCount++;
                }
                return summary;
            }

            if (installable.Count == 0)
            {
                return summary;
            }

            try
            {
                var installer = _session.CreateUpdateInstaller();
                var updateCollection = CreateUpdateCollection();

                // First, ensure updates are downloaded
                var needsDownload = installable.Where(u => !u.IsDownloaded).ToList();
                if (needsDownload.Count > 0)
                {
                    if (Logger.IsInitialized)
                        Logger.Instance.Info($"Downloading {needsDownload.Count} update(s) before installation...");

                    var downloadResult = Download(needsDownload, force, false, progress);
                    if (downloadResult.FailureCount > 0)
                    {
                        foreach (var failed in downloadResult.Results.Where(r => !r.Success))
                        {
                            summary.Results.Add(failed);
                            summary.FailureCount++;
                        }
                        installable = installable.Where(u => !downloadResult.Results.Any(r => r.Update == u && !r.Success)).ToList();
                    }
                }

                foreach (var update in installable)
                {
                    updateCollection.Add((IUpdate)update.ComObject);
                }

                if (updateCollection.Count == 0)
                {
                    return summary;
                }

                installer.Updates = updateCollection;
                installer.IsForced = force;

                progress?.StartBatch("install", updateCollection.Count);

                var result = installer.Install();

                // Check reboot required
                summary.RebootRequired = result.RebootRequired;

                // Process results
                for (int i = 0; i < updateCollection.Count; i++)
                {
                    var update = installable.FirstOrDefault(u => u.ComObject == updateCollection[i]);
                    if (update == null) continue;

                    var updateResult = result.GetUpdateResult(i);
                    var success = updateResult.ResultCode == OperationResultCode.orcSucceeded ||
                                  updateResult.ResultCode == OperationResultCode.orcSucceededWithErrors;

                    var opResult = new UpdateOperationResult
                    {
                        Update = update,
                        Success = success,
                        ResultCode = (int)updateResult.ResultCode,
                        ResultMessage = GetResultCodeString(updateResult.ResultCode),
                        HResult = updateResult.HResult
                    };

                    summary.Results.Add(opResult);
                    if (success)
                        summary.SuccessCount++;
                    else
                        summary.FailureCount++;

                    progress?.CompleteUpdate(success, opResult.ResultMessage);
                }

                progress?.CompleteBatch(summary);
                return summary;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Install failed: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region Uninstall Operations

        /// <summary>
        /// Uninstalls the specified updates.
        /// </summary>
        public OperationSummary Uninstall(List<UpdateInfo> updates, bool force, bool whatIf, ProgressReporter progress)
        {
            var summary = new OperationSummary { TotalCount = updates.Count };

            // Check if updates can be uninstalled
            var uninstallable = new List<UpdateInfo>();
            foreach (var update in updates)
            {
                if (!update.IsUninstallable && !force)
                {
                    summary.Results.Add(new UpdateOperationResult
                    {
                        Update = update,
                        Success = false,
                        ResultMessage = "Update cannot be uninstalled"
                    });
                    summary.FailureCount++;
                }
                else if (!update.IsInstalled)
                {
                    summary.Results.Add(new UpdateOperationResult
                    {
                        Update = update,
                        Success = false,
                        ResultMessage = "Update is not installed"
                    });
                    summary.FailureCount++;
                }
                else
                {
                    uninstallable.Add(update);
                }
            }

            if (whatIf)
            {
                if (Logger.IsInitialized)
                    Logger.Instance.Info("WhatIf mode: The following updates would be uninstalled:");

                foreach (var update in uninstallable)
                {
                    if (Logger.IsInitialized)
                        Logger.Instance.WriteOutput($"  - {update.PrimaryKB}: {update.Title}");

                    summary.Results.Add(new UpdateOperationResult
                    {
                        Update = update,
                        Success = true,
                        ResultMessage = "WhatIf: Would uninstall"
                    });
                    summary.SuccessCount++;
                }
                return summary;
            }

            if (uninstallable.Count == 0)
            {
                return summary;
            }

            try
            {
                var installer = _session.CreateUpdateInstaller();
                var updateCollection = CreateUpdateCollection();

                foreach (var update in uninstallable)
                {
                    updateCollection.Add((IUpdate)update.ComObject);
                }

                installer.Updates = updateCollection;
                installer.IsForced = force;

                progress?.StartBatch("uninstall", updateCollection.Count);

                var result = installer.Uninstall();

                summary.RebootRequired = result.RebootRequired;

                for (int i = 0; i < updateCollection.Count; i++)
                {
                    var update = uninstallable.FirstOrDefault(u => u.ComObject == updateCollection[i]);
                    if (update == null) continue;

                    var updateResult = result.GetUpdateResult(i);
                    var success = updateResult.ResultCode == OperationResultCode.orcSucceeded ||
                                  updateResult.ResultCode == OperationResultCode.orcSucceededWithErrors;

                    var opResult = new UpdateOperationResult
                    {
                        Update = update,
                        Success = success,
                        ResultCode = (int)updateResult.ResultCode,
                        ResultMessage = GetResultCodeString(updateResult.ResultCode),
                        HResult = updateResult.HResult
                    };

                    summary.Results.Add(opResult);
                    if (success)
                        summary.SuccessCount++;
                    else
                        summary.FailureCount++;

                    progress?.CompleteUpdate(success, opResult.ResultMessage);
                }

                progress?.CompleteBatch(summary);
                return summary;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Uninstall failed: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region History Operations

        /// <summary>
        /// Gets the total count of history entries.
        /// </summary>
        public int GetHistoryCount()
        {
            try
            {
                return _searcher.GetTotalHistoryCount();
            }
            catch (COMException ex)
            {
                if (Logger.IsInitialized)
                    Logger.Instance.Warn($"Failed to get history count: {GetComErrorMessage(ex)}");
                return 0;
            }
        }

        /// <summary>
        /// Gets update history.
        /// </summary>
        public List<HistoryEntry> GetHistory(int startIndex, int count)
        {
            try
            {
                var history = _searcher.QueryHistory(startIndex, count);
                var entries = new List<HistoryEntry>();

                for (int i = 0; i < history.Count; i++)
                {
                    var entry = history[i];
                    entries.Add(new HistoryEntry
                    {
                        Index = startIndex + i + 1,
                        Operation = GetOperationString(entry.Operation),
                        ResultCode = (int)entry.ResultCode,
                        Date = entry.Date,
                        Title = entry.Title,
                        UpdateId = entry.UpdateIdentity?.UpdateID,
                        ClientApplicationId = entry.ClientApplicationID,
                        ServiceId = entry.ServiceID,
                        HResult = entry.HResult,
                        SupportUrl = entry.SupportUrl
                    });
                }

                return entries;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Failed to query history: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region Service Operations

        /// <summary>
        /// Gets the list of registered update services.
        /// </summary>
        public List<ServiceInfo> GetServices()
        {
            try
            {
                var services = new List<ServiceInfo>();

                foreach (IUpdateService2 service in _serviceManager.Services)
                {
                    services.Add(new ServiceInfo
                    {
                        ServiceId = service.ServiceID,
                        Name = service.Name,
                        IsManaged = service.IsManaged,
                        IsDefaultAUService = service.IsDefaultAUService,
                        OffersWindowsUpdates = service.OffersWindowsUpdates,
                        IsRegisteredWithAU = service.IsRegisteredWithAU,
                        IsScanPackageService = service.IsScanPackageService,
                        CanRegisterWithAU = service.CanRegisterWithAU,
                        ServiceUrl = service.ServiceUrl,
                        SetupPrefix = service.SetupPrefix
                    });
                }

                return services;
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Failed to get services: {GetComErrorMessage(ex)}", ex);
            }
        }

        /// <summary>
        /// Removes an update service.
        /// </summary>
        public void RemoveService(string serviceId)
        {
            try
            {
                _serviceManager.RemoveService(serviceId);
                if (Logger.IsInitialized)
                    Logger.Instance.Info($"Service {serviceId} removed successfully.");
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"Failed to remove service: {GetComErrorMessage(ex)}", ex);
            }
        }

        #endregion

        #region Status Operations

        /// <summary>
        /// Gets system status.
        /// </summary>
        public bool IsRebootRequired()
        {
            try
            {
                return _systemInfo.RebootRequired;
            }
            catch (COMException ex)
            {
                if (Logger.IsInitialized)
                    Logger.Instance.Warn($"Failed to check reboot status: {GetComErrorMessage(ex)}");
                return false;
            }
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// Filters updates by selection expression.
        /// </summary>
        public List<UpdateInfo> SelectUpdates(List<UpdateInfo> updates, bool selectAll, string selectionType, string[] selectionValues)
        {
            if (selectAll)
            {
                return updates;
            }

            if (selectionType == "kb")
            {
                var selectedKBs = selectionValues.Select(kb =>
                    kb.StartsWith("KB", StringComparison.OrdinalIgnoreCase) ? kb : "KB" + kb
                ).ToList();

                return updates.Where(u =>
                    u.KBArticleIDs.Any(kb => selectedKBs.Any(sel =>
                        sel.Equals(kb, StringComparison.OrdinalIgnoreCase) ||
                        sel.Equals("KB" + kb, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            if (selectionType == "index")
            {
                var indices = new HashSet<int>();
                foreach (var val in selectionValues)
                {
                    int idx;
                    if (int.TryParse(val.Trim(), out idx))
                    {
                        indices.Add(idx);
                    }
                }

                return updates.Where(u => indices.Contains(u.Index)).ToList();
            }

            return new List<UpdateInfo>();
        }

        #endregion

        #region Private Helpers

        private void ConfigureSearcher(ServerMode serverMode, string serviceId)
        {
            switch (serverMode)
            {
                case ServerMode.MicrosoftUpdate:
                    _searcher.ServerSelection = ServerSelection.ssOthers;
                    // Microsoft Update service ID
                    _searcher.ServiceID = string.IsNullOrEmpty(serviceId)
                        ? "7971f918-a847-4430-9279-4a52d1efe18d"
                        : serviceId;
                    break;
                default:
                    _searcher.ServerSelection = ServerSelection.ssWindowsUpdate;
                    break;
            }
        }

        private UpdateInfo ConvertToUpdateInfo(IUpdate update, int index)
        {
            var info = new UpdateInfo
            {
                Index = index,
                Title = update.Title,
                MsrcSeverity = update.MsrcSeverity ?? "Unspecified",
                MaxDownloadSize = (long)update.MaxDownloadSize,
                IsDownloaded = update.IsDownloaded,
                IsInstalled = update.IsInstalled,
                IsHidden = update.IsHidden,
                IsMandatory = update.IsMandatory,
                IsUninstallable = update.IsUninstallable,
                IsBeta = update.IsBeta,
                AutoSelectOnWebSites = update.AutoSelectOnWebSites,
                EulaAccepted = update.EulaAccepted,
                UpdateId = update.Identity?.UpdateID,
                ComObject = update
            };

            // Get KB article IDs
            try
            {
                var kbCollection = update.KBArticleIDs;
                for (int i = 0; i < kbCollection.Count; i++)
                {
                    info.KBArticleIDs.Add("KB" + kbCollection[i]);
                }
            }
            catch { }

            // Get categories
            try
            {
                var categories = update.Categories;
                for (int i = 0; i < categories.Count; i++)
                {
                    info.Categories.Add(categories[i].Name);
                }
            }
            catch { }

            // Get installation behavior
            try
            {
                var behavior = update.InstallationBehavior;
                info.RebootBehavior = GetRebootBehaviorString(behavior.RebootBehavior);
                info.RebootRequired = behavior.RebootBehavior != InstallationRebootBehavior.irbNeverReboots;
            }
            catch { }

            return info;
        }

        private UpdateCollection CreateUpdateCollection()
        {
            return new UpdateCollection();
        }

        private static string GetResultCodeString(OperationResultCode code)
        {
            switch (code)
            {
                case OperationResultCode.orcNotStarted: return "Not Started";
                case OperationResultCode.orcInProgress: return "In Progress";
                case OperationResultCode.orcSucceeded: return "Succeeded";
                case OperationResultCode.orcSucceededWithErrors: return "Succeeded with Errors";
                case OperationResultCode.orcFailed: return "Failed";
                case OperationResultCode.orcAborted: return "Aborted";
                default: return $"Unknown ({(int)code})";
            }
        }

        private static string GetOperationString(tagUpdateOperation operation)
        {
            switch (operation)
            {
                case tagUpdateOperation.uoInstallation: return "Install";
                case tagUpdateOperation.uoUninstallation: return "Uninstall";
                default: return "Unknown";
            }
        }

        private static string GetRebootBehaviorString(InstallationRebootBehavior behavior)
        {
            switch (behavior)
            {
                case InstallationRebootBehavior.irbNeverReboots: return "Never";
                case InstallationRebootBehavior.irbAlwaysRequiresReboot: return "Always";
                case InstallationRebootBehavior.irbCanRequestReboot: return "May Request";
                default: return "Unknown";
            }
        }

        private static string GetComErrorMessage(COMException ex)
        {
            var hresult = ex.ErrorCode;
            var message = ex.Message;

            // Common WU error codes
            switch ((uint)hresult)
            {
                case 0x80240001: return "WU_E_NO_SERVICE - Windows Update Agent was unable to provide the service.";
                case 0x80240002: return "WU_E_MAX_CAPACITY_REACHED - The maximum capacity of the service was exceeded.";
                case 0x80240003: return "WU_E_UNKNOWN_ID - An ID cannot be found.";
                case 0x80240004: return "WU_E_NOT_INITIALIZED - The object could not be initialized.";
                case 0x80240005: return "WU_E_RANGEOVERLAP - The update handler requested a byte range overlapping a previously requested range.";
                case 0x80240006: return "WU_E_TOOMANYRANGES - The requested number of byte ranges exceeds the maximum number.";
                case 0x80240007: return "WU_E_INVALIDINDEX - The index to a collection was invalid.";
                case 0x80240008: return "WU_E_ITEMNOTFOUND - The key for the item queried could not be found.";
                case 0x80240009: return "WU_E_OPERATIONINPROGRESS - Another conflicting operation was in progress.";
                case 0x8024000A: return "WU_E_COULDNOTCANCEL - Cancellation of the operation was not allowed.";
                case 0x8024000B: return "WU_E_CALL_CANCELLED - Operation was cancelled.";
                case 0x8024000C: return "WU_E_NOOP - No operation was required.";
                case 0x8024000D: return "WU_E_XML_MISSINGDATA - Windows Update Agent could not find required information in the update's XML data.";
                case 0x8024000E: return "WU_E_XML_INVALID - Windows Update Agent found invalid information in the update's XML data.";
                case 0x8024000F: return "WU_E_CYCLE_DETECTED - Circular update relationships were detected in the metadata.";
                case 0x80240010: return "WU_E_TOO_DEEP_RELATION - Update relationships too deep to evaluate were evaluated.";
                case 0x80240011: return "WU_E_INVALID_RELATIONSHIP - An invalid update relationship was detected.";
                case 0x80240012: return "WU_E_REG_VALUE_INVALID - An invalid registry value was read.";
                case 0x80240013: return "WU_E_DUPLICATE_ITEM - Operation tried to add a duplicate item to a list.";
                case 0x80240016: return "WU_E_INSTALL_NOT_ALLOWED - Operation tried to install while another installation was in progress.";
                case 0x80240017: return "WU_E_NOT_APPLICABLE - Operation was not performed because there are no applicable updates.";
                case 0x80240018: return "WU_E_NO_USERTOKEN - Operation failed because a required user token is missing.";
                case 0x80240019: return "WU_E_EXCLUSIVE_INSTALL_CONFLICT - An exclusive update cannot be installed with other updates at the same time.";
                case 0x8024001A: return "WU_E_POLICY_NOT_SET - A policy value was not set.";
                case 0x8024001B: return "WU_E_SELFUPDATE_IN_PROGRESS - The operation could not be performed because the Windows Update Agent is self-updating.";
                case 0x8024001D: return "WU_E_INVALID_UPDATE - An update contains invalid metadata.";
                case 0x8024001E: return "WU_E_SERVICE_STOP - Operation did not complete because the service or system was being shut down.";
                case 0x8024001F: return "WU_E_NO_CONNECTION - Operation did not complete because the network connection was unavailable.";
                case 0x80240020: return "WU_E_NO_INTERACTIVE_USER - Operation did not complete because there is no logged-on interactive user.";
                case 0x80240021: return "WU_E_TIME_OUT - Operation did not complete because it timed out.";
                case 0x80240022: return "WU_E_ALL_UPDATES_FAILED - Operation failed for all the updates.";
                case 0x80240023: return "WU_E_EULAS_DECLINED - The license terms for all updates were declined.";
                case 0x80240024: return "WU_E_NO_UPDATE - There are no updates.";
                case 0x80240025: return "WU_E_USER_ACCESS_DISABLED - Group Policy settings prevented access to Windows Update.";
                case 0x80240026: return "WU_E_INVALID_UPDATE_TYPE - The type of update is invalid.";
                case 0x80240027: return "WU_E_URL_TOO_LONG - The URL exceeded the maximum length.";
                case 0x80240028: return "WU_E_UNINSTALL_NOT_ALLOWED - The update could not be uninstalled because the request did not originate from a WSUS server.";
                case 0x80240029: return "WU_E_INVALID_PRODUCT_LICENSE - Search may have missed some updates before there is an unlicensed application on the system.";
                case 0x8024002A: return "WU_E_MISSING_HANDLER - A component required to detect applicable updates was missing.";
                case 0x8024002B: return "WU_E_LEGACYSERVER - An operation did not complete because it requires a newer version of server.";
                case 0x8024002C: return "WU_E_BIN_SOURCE_ABSENT - A delta-compressed update could not be installed because it required the source.";
                case 0x8024002D: return "WU_E_SOURCE_ABSENT - A full-file update could not be installed because it required the source.";
                case 0x8024002E: return "WU_E_WU_DISABLED - Access to an unmanaged server is not allowed.";
                case 0x8024002F: return "WU_E_CALL_CANCELLED_BY_POLICY - Operation did not complete because the DisableWindowsUpdateAccess policy was set.";
                case 0x80240030: return "WU_E_INVALID_PROXY_SERVER - The format of the proxy list was invalid.";
                case 0x80240031: return "WU_E_INVALID_FILE - The file is in the wrong format.";
                case 0x80240032: return "WU_E_INVALID_CRITERIA - The search criteria string was invalid.";
                case 0x80240033: return "WU_E_EULA_UNAVAILABLE - License terms could not be downloaded.";
                case 0x80240034: return "WU_E_DOWNLOAD_FAILED - Update failed to download.";
                case 0x80240035: return "WU_E_UPDATE_NOT_PROCESSED - The update was not processed.";
                case 0x80240036: return "WU_E_INVALID_OPERATION - The object's current state did not allow the operation.";
                case 0x80240037: return "WU_E_NOT_SUPPORTED - The functionality for the operation is not supported.";
                case 0x80240FFF: return "WU_E_UNEXPECTED - An operation failed due to reasons not covered by another error code.";
                case 0x80070005: return "E_ACCESSDENIED - Access denied. Administrator privileges may be required.";
            }

            return $"HRESULT 0x{hresult:X8}: {message}";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_searcher != null)
            {
                Marshal.ReleaseComObject(_searcher);
                _searcher = null;
            }

            if (_serviceManager != null)
            {
                Marshal.ReleaseComObject(_serviceManager);
                _serviceManager = null;
            }

            if (_systemInfo != null)
            {
                Marshal.ReleaseComObject(_systemInfo);
                _systemInfo = null;
            }

            if (_session != null)
            {
                Marshal.ReleaseComObject(_session);
                _session = null;
            }
        }

        #endregion
    }
}
