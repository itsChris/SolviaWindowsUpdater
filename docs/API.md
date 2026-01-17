# API Reference: SolviaWindowsUpdater

This document provides detailed API reference for developers extending or integrating with SolviaWindowsUpdater.

---

## Table of Contents

- [Exit Codes](#exit-codes)
- [JSON Output Schemas](#json-output-schemas)
- [WUA Search Criteria](#wua-search-criteria)
- [Selection Syntax](#selection-syntax)
- [Internal Classes](#internal-classes)

---

## Exit Codes

Use these exit codes for scripting and automation.

| Code | Constant | Description | Typical Cause |
|------|----------|-------------|---------------|
| 0 | `Success` | Operation completed successfully | All operations succeeded |
| 1 | `PartialSuccess` | Some operations failed | Some updates failed, others succeeded |
| 2 | `ValidationError` | Invalid arguments | Bad syntax, invalid combinations |
| 3 | `ElevationRequired` | Admin privileges needed | Running non-elevated |
| 4 | `NoUpdatesFound` | No matching updates | Search returned empty |
| 5 | `WuaError` | Windows Update API error | Service unavailable, network issues |
| 6 | `EulaNotAccepted` | EULA acceptance required | Missing `--accept-eulas` |
| 7 | `UpdateNotReady` | Update not downloaded | Install without download |
| 8 | `UpdateNotUninstallable` | Cannot uninstall | Update doesn't support uninstall |
| 9 | `UserCancelled` | Operation cancelled | User intervention |
| 10 | `UnexpectedError` | Unexpected error | Unhandled exception |

### Scripting Examples

**CMD:**
```cmd
SolviaWindowsUpdater.exe install --all --accept-eulas --noreboot
if %ERRORLEVEL% EQU 0 echo Success
if %ERRORLEVEL% EQU 1 echo Partial success
if %ERRORLEVEL% EQU 3 echo Run as admin
if %ERRORLEVEL% GEQ 2 echo Error: %ERRORLEVEL%
```

**PowerShell:**
```powershell
& .\SolviaWindowsUpdater.exe install --all --accept-eulas --noreboot
switch ($LASTEXITCODE) {
    0 { "Success" }
    1 { "Partial success" }
    3 { "Elevation required" }
    default { "Error: $LASTEXITCODE" }
}
```

---

## JSON Output Schemas

### Search Output (--output json)

Minimal format for common use cases:

```json
[
  {
    "index": 1,
    "kb": "KB5001234",
    "title": "2024-01 Cumulative Update for Windows 11",
    "size": 548864000,
    "isDownloaded": false,
    "isInstalled": false
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| index | int | 1-based index in results |
| kb | string | Primary KB article ID |
| title | string | Update title |
| size | long | Max download size in bytes |
| isDownloaded | bool | Whether cached locally |
| isInstalled | bool | Whether installed |

### Search Output (--output json-full)

Verbose format with all properties:

```json
[
  {
    "index": 1,
    "title": "2024-01 Cumulative Update for Windows 11",
    "kbArticleIds": ["KB5001234"],
    "updateId": "12345678-1234-1234-1234-123456789abc",
    "categories": ["Security Updates", "Windows 11"],
    "msrcSeverity": "Critical",
    "maxDownloadSize": 548864000,
    "isDownloaded": false,
    "isInstalled": false,
    "isHidden": false,
    "isMandatory": true,
    "isUninstallable": true,
    "isBeta": false,
    "eulaAccepted": false,
    "rebootBehavior": "May Request",
    "autoSelectOnWebSites": true
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| index | int | 1-based index |
| title | string | Full update title |
| kbArticleIds | string[] | All KB IDs |
| updateId | string | Update GUID |
| categories | string[] | Category names |
| msrcSeverity | string | MSRC severity rating |
| maxDownloadSize | long | Size in bytes |
| isDownloaded | bool | Cached locally |
| isInstalled | bool | Currently installed |
| isHidden | bool | Hidden from results |
| isMandatory | bool | Required update |
| isUninstallable | bool | Can be removed |
| isBeta | bool | Beta/preview update |
| eulaAccepted | bool | EULA accepted |
| rebootBehavior | string | Never/May Request/Always |
| autoSelectOnWebSites | bool | Auto-selected on WU site |

### History Output (--output json-full)

```json
[
  {
    "index": 1,
    "operation": "Install",
    "resultCode": 2,
    "result": "Succeeded",
    "date": "2024-01-15T10:30:45",
    "title": "Security Update for Windows 11",
    "updateId": "12345678-1234-1234-1234-123456789abc",
    "clientApplicationId": "AutomaticUpdates",
    "serviceId": "7971f918-a847-4430-9279-4a52d1efe18d",
    "hresult": 0,
    "supportUrl": "https://support.microsoft.com/kb/5001234"
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| index | int | History entry index |
| operation | string | Install or Uninstall |
| resultCode | int | Operation result code |
| result | string | Human-readable result |
| date | string | ISO 8601 date/time |
| title | string | Update title |
| updateId | string | Update GUID |
| clientApplicationId | string | Installing application |
| serviceId | string | Update service GUID |
| hresult | int | HRESULT if failed |
| supportUrl | string | Support article URL |

### Services Output

```json
[
  {
    "serviceId": "7971f918-a847-4430-9279-4a52d1efe18d",
    "name": "Microsoft Update",
    "isManaged": false,
    "isDefaultAUService": false,
    "offersWindowsUpdates": true,
    "isRegisteredWithAU": true,
    "isScanPackageService": false,
    "canRegisterWithAU": true,
    "serviceUrl": "https://fe2.update.microsoft.com/v6/",
    "setupPrefix": ""
  }
]
```

---

## WUA Search Criteria

### Syntax

```
<property> <operator> <value> [AND|OR <property> <operator> <value>]...
```

### Properties

| Property | Type | Values |
|----------|------|--------|
| IsInstalled | int | 0 (not installed), 1 (installed) |
| IsHidden | int | 0 (visible), 1 (hidden) |
| IsPresent | int | 0 (not present), 1 (present) |
| IsBeta | int | 0 (release), 1 (beta) |
| IsAssigned | int | 0 (not assigned), 1 (assigned by policy) |
| AutoSelectOnWebSites | int | 0 (optional), 1 (auto-selected) |
| BrowseOnly | int | 0 (important), 1 (optional) |
| RebootRequired | int | 0 (no reboot), 1 (reboot required) |
| Type | string | 'Software', 'Driver' |
| DeploymentAction | string | 'Installation', 'Uninstallation' |
| CategoryIDs | collection | Category GUIDs |
| UpdateID | string | Specific update GUID |
| RevisionNumber | int | Update revision |

### Operators

| Operator | Usage |
|----------|-------|
| = | Equality: `IsInstalled=0` |
| != | Inequality: `IsHidden!=1` |
| contains | Collection membership: `CategoryIDs contains 'GUID'` |
| AND | Logical AND |
| OR | Logical OR |

### Category GUIDs

| GUID | Category |
|------|----------|
| E6CF1350-C01B-414D-A61F-263D14D133B4 | Critical Updates |
| 0FA1201D-4330-4FA8-8AE9-B877473B6441 | Security Updates |
| CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83 | Updates |
| E0789628-CE08-4437-BE74-2495B842F43B | Update Rollups |
| B4832BD8-E735-4761-8DAF-37F882276DAB | Service Packs |
| EBFC1FC5-71A4-4F7B-9ACA-3B9A503104A0 | Definition Updates |
| 28BC880E-0592-4CBF-8F95-C79B17911D5F | Feature Packs |
| 3689BDC8-B205-4AF4-8D4A-A63924C5E9D5 | Drivers |
| B54E7D24-7ADD-428F-8B75-90A396FA584F | Tools |

### Example Queries

```
# All available updates
IsInstalled=0

# Security updates only
IsInstalled=0 AND CategoryIDs contains '0FA1201D-4330-4FA8-8AE9-B877473B6441'

# Optional updates
IsInstalled=0 AND BrowseOnly=1

# Drivers only
IsInstalled=0 AND Type='Driver'

# Installed and uninstallable
IsInstalled=1

# Hidden updates
IsHidden=1
```

---

## Selection Syntax

### KB Selection

```
--select kb:KB5001234
--select kb:KB5001234,KB5001235,KB5001236
--select kb:5001234          # KB prefix optional
```

### Index Selection

```
--select index:1
--select index:1,2,3
--select index:1,3,5,7,9
```

### Select All

```
--all
```

**Note:** `--all` and `--select` are mutually exclusive.

---

## Internal Classes

### UpdateInfo

Represents a Windows Update.

```csharp
public class UpdateInfo
{
    public int Index { get; set; }
    public string Title { get; set; }
    public List<string> KBArticleIDs { get; set; }
    public string UpdateId { get; set; }
    public List<string> Categories { get; set; }
    public string MsrcSeverity { get; set; }
    public long MaxDownloadSize { get; set; }
    public bool IsDownloaded { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsHidden { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsUninstallable { get; set; }
    public bool IsBeta { get; set; }
    public bool EulaAccepted { get; set; }
    public string RebootBehavior { get; set; }
    public bool AutoSelectOnWebSites { get; set; }
    public object ComObject { get; set; }  // IUpdate reference
}
```

### HistoryEntry

Represents an update history record.

```csharp
public class HistoryEntry
{
    public int Index { get; set; }
    public string Operation { get; set; }
    public int ResultCode { get; set; }
    public DateTime Date { get; set; }
    public string Title { get; set; }
    public string UpdateId { get; set; }
    public string ClientApplicationId { get; set; }
    public string ServiceId { get; set; }
    public int HResult { get; set; }
    public string SupportUrl { get; set; }
}
```

### ServiceInfo

Represents a Windows Update service.

```csharp
public class ServiceInfo
{
    public string ServiceId { get; set; }
    public string Name { get; set; }
    public bool IsManaged { get; set; }
    public bool IsDefaultAUService { get; set; }
    public bool OffersWindowsUpdates { get; set; }
    public bool IsRegisteredWithAU { get; set; }
    public bool IsScanPackageService { get; set; }
    public bool CanRegisterWithAU { get; set; }
    public string ServiceUrl { get; set; }
    public string SetupPrefix { get; set; }
}
```

### OperationSummary

Returned by Download/Install/Uninstall operations.

```csharp
public class OperationSummary
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public bool RebootRequired { get; set; }
    public List<UpdateOperationResult> Results { get; set; }
}

public class UpdateOperationResult
{
    public UpdateInfo Update { get; set; }
    public bool Success { get; set; }
    public int ResultCode { get; set; }
    public string ResultMessage { get; set; }
    public int HResult { get; set; }
}
```

---

## WUApiLib COM Interfaces

### IUpdateSession3

```csharp
IUpdateSearcher CreateUpdateSearcher();
IUpdateDownloader CreateUpdateDownloader();
IUpdateInstaller CreateUpdateInstaller();
```

### IUpdateSearcher

```csharp
ISearchResult Search(string criteria);
IUpdateHistoryEntryCollection QueryHistory(int startIndex, int count);
int GetTotalHistoryCount();
ServerSelection ServerSelection { get; set; }
string ServiceID { get; set; }
```

### IUpdateDownloader

```csharp
UpdateCollection Updates { get; set; }
bool IsForced { get; set; }
IDownloadResult Download();
```

### IUpdateInstaller

```csharp
UpdateCollection Updates { get; set; }
bool IsForced { get; set; }
IInstallationResult Install();
IInstallationResult Uninstall();
```

### ISystemInformation

```csharp
bool RebootRequired { get; }
```

---

## Error Codes

### Common WUA HRESULT Values

| HRESULT | Name | Description |
|---------|------|-------------|
| 0x80240001 | WU_E_NO_SERVICE | Service unavailable |
| 0x80240016 | WU_E_INSTALL_NOT_ALLOWED | Another install in progress |
| 0x80240017 | WU_E_NOT_APPLICABLE | No applicable updates |
| 0x8024001F | WU_E_NO_CONNECTION | Network unavailable |
| 0x80240022 | WU_E_ALL_UPDATES_FAILED | All operations failed |
| 0x80240032 | WU_E_INVALID_CRITERIA | Bad search criteria |
| 0x80240034 | WU_E_DOWNLOAD_FAILED | Download failed |
| 0x80070005 | E_ACCESSDENIED | Access denied |
