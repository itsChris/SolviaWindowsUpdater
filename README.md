# SolviaWindowsUpdater

A production-ready Windows Update Agent command-line tool built on .NET Framework 4.5 using the native WUApiLib COM Interop. Zero external dependencies.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Command Reference](#command-reference)
  - [search](#search)
  - [download](#download)
  - [install](#install)
  - [uninstall](#uninstall)
  - [history](#history)
  - [services](#services)
  - [status](#status)
- [Global Options](#global-options)
- [Search Criteria Syntax](#search-criteria-syntax)
- [Selection Syntax](#selection-syntax)
- [Output Formats](#output-formats)
- [Server Modes](#server-modes)
- [Exit Codes](#exit-codes)
- [Logging](#logging)
- [Architecture](#architecture)
- [WUApiLib Interfaces](#wuapilib-interfaces)
- [Automation & Scripting](#automation--scripting)
- [Troubleshooting](#troubleshooting)
- [Security Considerations](#security-considerations)
- [Known Limitations](#known-limitations)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

**SolviaWindowsUpdater** is a lightweight, standalone CLI tool for managing Windows Updates programmatically. It wraps the Windows Update Agent API (WUApiLib) to provide:

- Full control over the update lifecycle (search, download, install, uninstall)
- Machine-readable output (JSON) for automation pipelines
- Granular selection of updates by KB article ID or index
- Support for both Windows Update and Microsoft Update services
- Real-time progress reporting for long-running operations
- Comprehensive logging for troubleshooting

Unlike PowerShell modules that depend on external libraries, SolviaWindowsUpdater uses only the native Windows Update Agent COM API, ensuring maximum compatibility and minimal deployment footprint.

---

## Features

| Feature | Description |
|---------|-------------|
| **Search** | Query available updates with flexible WUA criteria syntax |
| **Download** | Download updates selectively or in bulk with progress tracking |
| **Install** | Install updates with EULA acceptance and reboot control |
| **Uninstall** | Remove installed updates (where supported by the update) |
| **History** | View complete update installation history |
| **Services** | List and manage registered update services |
| **Status** | Check if a system reboot is pending |
| **JSON Output** | Machine-readable output for CI/CD and automation |
| **Dry Run** | Preview operations with `--whatif` before execution |
| **Partial Failure Handling** | Continue processing on individual failures |
| **Real-time Progress** | Live progress updates during download/install |
| **Dual Logging** | Console output with optional file logging |

---

## Requirements

| Requirement | Minimum Version |
|-------------|-----------------|
| **Operating System** | Windows 7 SP1 / Windows Server 2008 R2 |
| **.NET Framework** | 4.5 or later |
| **Windows Update Service** | Must be running (`wuauserv`) |
| **Privileges** | Administrator for download/install/uninstall |

### Verify Prerequisites

```cmd
:: Check .NET Framework version
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Version

:: Check Windows Update service status
sc query wuauserv
```

---

## Installation

### Option 1: Download Release Binary

Download the latest `SolviaWindowsUpdater.exe` from the [Releases](../../releases) page.

### Option 2: Build from Source

```cmd
:: Clone the repository
git clone https://github.com/solvia/SolviaWindowsUpdater.git
cd SolviaWindowsUpdater

:: Build with MSBuild (Visual Studio 2022)
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
    SolviaWindowsUpdater.sln /p:Configuration=Release

:: Or with .NET Framework MSBuild
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe ^
    SolviaWindowsUpdater.sln /p:Configuration=Release

:: Output location
bin\Release\SolviaWindowsUpdater.exe
```

### Option 3: Visual Studio

1. Open `SolviaWindowsUpdater.sln` in Visual Studio 2019/2022
2. Set configuration to `Release`
3. Build solution (F6 or Ctrl+Shift+B)

---

## Quick Start

```cmd
:: Search for available updates
SolviaWindowsUpdater search

:: Download all available updates (requires admin)
SolviaWindowsUpdater download --all

:: Install all updates accepting EULAs (requires admin)
SolviaWindowsUpdater install --all --accept-eulas

:: Check if reboot is required
SolviaWindowsUpdater status

:: View update history
SolviaWindowsUpdater history
```

---

## Command Reference

### search

Search for available Windows updates using WUA criteria.

```
SolviaWindowsUpdater search [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--criteria` | string | `IsInstalled=0` | WUA search criteria expression |
| `--include-hidden` | flag | false | Include hidden updates |
| `--max-results` | int | 50 | Maximum results to return (1-500) |
| `--output` | enum | table | Output format: `table`, `json`, `json-full` |
| `--server` | enum | windowsupdate | Server: `windowsupdate`, `microsoftupdate` |

**Examples:**

```cmd
:: List all available (not installed) updates
SolviaWindowsUpdater search

:: Search for security updates only
SolviaWindowsUpdater search --criteria "IsInstalled=0 AND Type='Software' AND CategoryIDs contains 'E6CF1350-C01B-414D-A61F-263D14D133B4'"

:: Search for optional/driver updates
SolviaWindowsUpdater search --criteria "IsInstalled=0 AND BrowseOnly=1"

:: Include Office and driver updates from Microsoft Update
SolviaWindowsUpdater search --server microsoftupdate

:: Output as JSON for scripting
SolviaWindowsUpdater search --output json

:: Get detailed JSON with all properties
SolviaWindowsUpdater search --output json-full --max-results 100

:: Include hidden updates in results
SolviaWindowsUpdater search --include-hidden

:: Search for a specific KB
SolviaWindowsUpdater search --criteria "IsInstalled=0" | findstr "KB5001234"
```

---

### download

Download selected updates to the local cache. Performs an implicit search first.

```
SolviaWindowsUpdater download <--all | --select <expr>> [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--all` | flag | - | Download all updates from search |
| `--select` | string | - | Selection expression (see [Selection Syntax](#selection-syntax)) |
| `--criteria` | string | `IsInstalled=0` | WUA search criteria |
| `--force` | flag | false | Force re-download even if cached |
| `--whatif` | flag | false | Show what would be downloaded |
| `--server` | enum | windowsupdate | Server mode |

**Requires:** Administrator privileges

**Examples:**

```cmd
:: Download all available updates
SolviaWindowsUpdater download --all

:: Download a specific KB
SolviaWindowsUpdater download --select kb:KB5001234

:: Download multiple KBs
SolviaWindowsUpdater download --select kb:KB5001234,KB5001235,KB5001236

:: Download by index from search results
SolviaWindowsUpdater download --select index:1,2,3

:: Preview what would be downloaded
SolviaWindowsUpdater download --all --whatif

:: Force re-download of cached updates
SolviaWindowsUpdater download --all --force

:: Download from Microsoft Update (includes Office, drivers)
SolviaWindowsUpdater download --all --server microsoftupdate
```

---

### install

Install selected updates. Downloads first if not already cached. Performs an implicit search first.

```
SolviaWindowsUpdater install <--all | --select <expr>> --accept-eulas [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--all` | flag | - | Install all updates from search |
| `--select` | string | - | Selection expression |
| `--accept-eulas` | flag | **required** | Accept End User License Agreements |
| `--criteria` | string | `IsInstalled=0` | WUA search criteria |
| `--force` | flag | false | Force reinstallation |
| `--noreboot` | flag | false | Suppress reboot prompt |
| `--whatif` | flag | false | Show what would be installed |
| `--server` | enum | windowsupdate | Server mode |

**Requires:** Administrator privileges

**Examples:**

```cmd
:: Install all available updates
SolviaWindowsUpdater install --all --accept-eulas

:: Install a specific KB
SolviaWindowsUpdater install --select kb:KB5001234 --accept-eulas

:: Install without prompting for reboot
SolviaWindowsUpdater install --all --accept-eulas --noreboot

:: Preview installation
SolviaWindowsUpdater install --all --accept-eulas --whatif

:: Force reinstall
SolviaWindowsUpdater install --select kb:KB5001234 --accept-eulas --force

:: Install from Microsoft Update
SolviaWindowsUpdater install --all --accept-eulas --server microsoftupdate
```

---

### uninstall

Uninstall previously installed updates (where uninstallation is supported).

```
SolviaWindowsUpdater uninstall --select <expr> [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--select` | string | **required** | Selection expression (kb: format) |
| `--force` | flag | false | Attempt uninstall even if marked as non-uninstallable |
| `--noreboot` | flag | false | Suppress reboot prompt |
| `--whatif` | flag | false | Show what would be uninstalled |

**Requires:** Administrator privileges

**Note:** Not all updates support uninstallation. Check the `IsUninstallable` property in search results.

**Examples:**

```cmd
:: Uninstall a specific KB
SolviaWindowsUpdater uninstall --select kb:KB5001234

:: Uninstall without reboot prompt
SolviaWindowsUpdater uninstall --select kb:KB5001234 --noreboot

:: Preview uninstallation
SolviaWindowsUpdater uninstall --select kb:KB5001234 --whatif

:: Force uninstall attempt
SolviaWindowsUpdater uninstall --select kb:KB5001234 --force
```

---

### history

View Windows Update installation history.

```
SolviaWindowsUpdater history [options]
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--count` | int | 0 (all) | Number of entries (0 = all) |
| `--start-index` | int | 0 | Starting index in history |
| `--output` | enum | table | Output format: `table`, `json`, `json-full` |

**Examples:**

```cmd
:: View all history entries
SolviaWindowsUpdater history

:: View last 50 entries
SolviaWindowsUpdater history --count 50

:: View entries 100-150
SolviaWindowsUpdater history --start-index 100 --count 50

:: Export history as JSON
SolviaWindowsUpdater history --output json-full > history.json

:: Find failed installations
SolviaWindowsUpdater history | findstr "Failed"
```

---

### services

List or remove registered Windows Update services.

```
SolviaWindowsUpdater services <list | remove> [options]
```

#### services list

List all registered update services.

```cmd
SolviaWindowsUpdater services list
```

#### services remove

Remove a registered update service.

```cmd
SolviaWindowsUpdater services remove --service-id <guid>
```

| Option | Type | Description |
|--------|------|-------------|
| `--service-id` | GUID | **required** Service ID to remove |

**Requires:** Administrator privileges (for remove)

**Examples:**

```cmd
:: List all registered services
SolviaWindowsUpdater services list

:: Remove a service by GUID
SolviaWindowsUpdater services remove --service-id 7971f918-a847-4430-9279-4a52d1efe18d
```

**Common Service IDs:**

| Service ID | Name |
|------------|------|
| `7971f918-a847-4430-9279-4a52d1efe18d` | Microsoft Update |
| `9482f4b4-e343-43b6-b170-9a65bc822c77` | Windows Update |
| `855e8a7c-ecb4-4ca3-b045-1dfa50104289` | Windows Store (Dcat Flighting) |

---

### status

Check if a system reboot is required to complete pending update operations.

```
SolviaWindowsUpdater status
```

**Example Output:**

```
System Status:
==============

  Reboot Required: YES

  A system reboot is pending to complete previous update operations.
```

---

## Global Options

These options apply to all commands:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `-h`, `--help` | flag | - | Show help information |
| `-v`, `--version` | flag | - | Show version information |
| `--log-path` | string | `%SystemDrive%\Solvia\Logs\Solvia.WuaCli.log` | Path to log file |
| `--log-level` | enum | Info | Log level: `Trace`, `Debug`, `Info`, `Warn`, `Error` |
| `--timeout-seconds` | int | 300 | Operation timeout in seconds (1-3600) |
| `--server` | enum | windowsupdate | Server mode (see [Server Modes](#server-modes)) |
| `--service-id` | GUID | - | Target specific service (requires `--server microsoftupdate`) |

**Examples:**

```cmd
:: Enable debug logging
SolviaWindowsUpdater search --log-level Debug

:: Custom log file location
SolviaWindowsUpdater install --all --accept-eulas --log-path "C:\Logs\wu-install.log"

:: Extended timeout for slow connections
SolviaWindowsUpdater download --all --timeout-seconds 1800
```

---

## Search Criteria Syntax

The `--criteria` option uses the Windows Update Agent search criteria syntax. This is a SQL-like expression language.

### Basic Syntax

```
<property> <operator> <value>
```

### Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equals | `IsInstalled=0` |
| `!=` | Not equals | `IsHidden!=1` |
| `contains` | Contains (for collections) | `CategoryIDs contains 'GUID'` |

### Logical Operators

| Operator | Description |
|----------|-------------|
| `AND` | Both conditions must be true |
| `OR` | Either condition must be true |

### Common Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsInstalled` | int | 0 = not installed, 1 = installed |
| `IsHidden` | int | 0 = visible, 1 = hidden |
| `IsAssigned` | int | 1 = assigned by admin policy |
| `IsBeta` | int | 1 = beta update |
| `IsPresent` | int | 1 = present on system |
| `AutoSelectOnWebSites` | int | 1 = auto-selected on Windows Update website |
| `BrowseOnly` | int | 1 = optional update (not auto-selected) |
| `RebootRequired` | int | 1 = reboot required |
| `Type` | string | `'Software'` or `'Driver'` |
| `CategoryIDs` | collection | Contains category GUIDs |
| `UpdateID` | string | Specific update GUID |
| `RevisionNumber` | int | Update revision |
| `DeploymentAction` | string | `'Installation'` or `'Uninstallation'` |

### Category GUIDs

| GUID | Category |
|------|----------|
| `E6CF1350-C01B-414D-A61F-263D14D133B4` | Critical Updates |
| `0FA1201D-4330-4FA8-8AE9-B877473B6441` | Security Updates |
| `CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83` | Updates |
| `E0789628-CE08-4437-BE74-2495B842F43B` | Update Rollups |
| `B4832BD8-E735-4761-8DAF-37F882276DAB` | Service Packs |
| `EBFC1FC5-71A4-4F7B-9ACA-3B9A503104A0` | Definition Updates |
| `28BC880E-0592-4CBF-8F95-C79B17911D5F` | Feature Packs |
| `3689BDC8-B205-4AF4-8D4A-A63924C5E9D5` | Drivers |
| `B54E7D24-7ADD-428F-8B75-90A396FA584F` | Tools |
| `5C9376AB-8CE6-464A-B136-22113DD69801` | Guidance |

### Example Criteria Expressions

```cmd
:: Available (not installed) updates
--criteria "IsInstalled=0"

:: Installed updates
--criteria "IsInstalled=1"

:: Security updates only
--criteria "IsInstalled=0 AND CategoryIDs contains '0FA1201D-4330-4FA8-8AE9-B877473B6441'"

:: Critical updates only
--criteria "IsInstalled=0 AND CategoryIDs contains 'E6CF1350-C01B-414D-A61F-263D14D133B4'"

:: Optional updates (drivers, feature packs)
--criteria "IsInstalled=0 AND BrowseOnly=1"

:: Non-optional (important) updates
--criteria "IsInstalled=0 AND BrowseOnly=0"

:: Software updates only (no drivers)
--criteria "IsInstalled=0 AND Type='Software'"

:: Driver updates only
--criteria "IsInstalled=0 AND Type='Driver'"

:: Hidden updates
--criteria "IsHidden=1"

:: Updates requiring reboot
--criteria "IsInstalled=0 AND RebootRequired=1"

:: Specific update by GUID
--criteria "UpdateID='12345678-1234-1234-1234-123456789abc'"

:: Combined criteria
--criteria "IsInstalled=0 AND Type='Software' AND BrowseOnly=0 AND IsHidden=0"
```

---

## Selection Syntax

The `--select` option supports two selection modes:

### By KB Article ID

```cmd
--select kb:KB5001234
--select kb:KB5001234,KB5001235,KB5001236
--select kb:5001234                          # "KB" prefix is optional
```

### By Index

Reference updates by their index number from search results:

```cmd
--select index:1
--select index:1,2,3,4,5
--select index:1,3,5,7
```

### Select All

Use `--all` instead of `--select` to select all updates:

```cmd
--all
```

**Note:** `--all` and `--select` are mutually exclusive.

---

## Output Formats

Three output formats are available for `search` and `history` commands:

### Table (Default)

Human-readable tabular format with auto-sized columns:

```
    # | KB         | Title                                              | Size       | Status
------+------------+----------------------------------------------------+------------+-----------
    1 | KB5001234  | 2024-01 Cumulative Update for Windows 11           | 523.5 MB   | Available
    2 | KB5001235  | Security Update for .NET Framework                 | 12.3 MB    | Available
```

### JSON

Minimal JSON output with essential properties:

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

### JSON-Full

Verbose JSON with all available properties:

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

---

## Server Modes

| Mode | Description |
|------|-------------|
| `windowsupdate` | **Default.** Windows Update service. Provides OS updates only. |
| `microsoftupdate` | Microsoft Update service. Includes Office, drivers, and other Microsoft products. |

**Examples:**

```cmd
:: Use Windows Update (default)
SolviaWindowsUpdater search

:: Use Microsoft Update for broader coverage
SolviaWindowsUpdater search --server microsoftupdate

:: Target a specific service by GUID
SolviaWindowsUpdater search --server microsoftupdate --service-id 7971f918-a847-4430-9279-4a52d1efe18d
```

---

## Exit Codes

| Code | Constant | Description |
|------|----------|-------------|
| 0 | `Success` | Operation completed successfully |
| 1 | `PartialSuccess` | Some operations failed, others succeeded |
| 2 | `ValidationError` | Invalid arguments or parameter combinations |
| 3 | `ElevationRequired` | Administrator privileges required |
| 4 | `NoUpdatesFound` | No updates matched the search criteria |
| 5 | `WuaError` | Windows Update Agent API error |
| 6 | `EulaNotAccepted` | EULA acceptance required but not provided |
| 7 | `UpdateNotReady` | Update not downloaded (for install) |
| 8 | `UpdateNotUninstallable` | Update cannot be uninstalled |
| 9 | `UserCancelled` | Operation cancelled by user |
| 10 | `UnexpectedError` | Unexpected/unhandled error |

**Usage in Scripts:**

```cmd
SolviaWindowsUpdater install --all --accept-eulas --noreboot
if %ERRORLEVEL% EQU 0 (
    echo Installation successful
) else if %ERRORLEVEL% EQU 1 (
    echo Partial success - check logs
) else if %ERRORLEVEL% EQU 3 (
    echo Run as administrator
) else (
    echo Error: %ERRORLEVEL%
)
```

```powershell
$result = & SolviaWindowsUpdater.exe install --all --accept-eulas --noreboot
switch ($LASTEXITCODE) {
    0 { Write-Host "Success" -ForegroundColor Green }
    1 { Write-Host "Partial success" -ForegroundColor Yellow }
    3 { Write-Host "Requires admin" -ForegroundColor Red }
    default { Write-Host "Error: $LASTEXITCODE" -ForegroundColor Red }
}
```

---

## Logging

### Log Destinations

- **Console:** Always enabled, with colored output for different log levels
- **File:** Written to configurable path (default: `%SystemDrive%\Solvia\Logs\Solvia.WuaCli.log`)

### Log Levels

| Level | Description |
|-------|-------------|
| `Trace` | Most verbose, includes internal details |
| `Debug` | Diagnostic information |
| `Info` | General operational messages (default) |
| `Warn` | Warning conditions |
| `Error` | Error conditions |

### Log Format

```
[2025-01-17 10:30:45.123] [INFO ] Searching for updates with criteria: IsInstalled=0
[2025-01-17 10:30:46.456] [DEBUG] Found 15 updates
[2025-01-17 10:30:46.789] [WARN ] Update KB5001234 requires EULA acceptance
[2025-01-17 10:30:47.012] [ERROR] Download failed: WU_E_DOWNLOAD_FAILED
```

### Log File Rotation

Log files are not automatically rotated. For production use, configure external log rotation or use a custom log path per session:

```cmd
SolviaWindowsUpdater install --all --accept-eulas ^
    --log-path "C:\Logs\wu-%DATE:~-4,4%%DATE:~-10,2%%DATE:~-7,2%.log"
```

---

## Architecture

```
SolviaWindowsUpdater/
├── SolviaWindowsUpdater.sln          # Visual Studio solution
├── SolviaWindowsUpdater.csproj       # Project file with COM reference
├── Program.cs                        # Entry point, command routing
│
├── Cli/                              # Command-line interface layer
│   ├── CliSpec.cs                    # Single source of truth for CLI spec
│   ├── ArgParser.cs                  # Argument parsing without external libs
│   ├── HelpGenerator.cs              # Dynamic help text from CliSpec
│   ├── Validator.cs                  # Validation rules engine
│   └── ParsedArgs.cs                 # Parsed argument container
│
├── Core/                             # Core infrastructure
│   ├── Logger.cs                     # Dual-output logging (console + file)
│   ├── ExitCodes.cs                  # Exit code constants
│   └── AdminHelper.cs                # Administrator privilege detection
│
├── Wua/                              # Windows Update Agent wrapper
│   ├── WuaClient.cs                  # Main WUApiLib client (~800 lines)
│   ├── UpdateInfo.cs                 # Update data model
│   ├── HistoryEntry.cs               # History entry model
│   ├── ServiceInfo.cs                # Service info model
│   └── ProgressReporter.cs           # Real-time progress reporting
│
├── Output/                           # Output formatting
│   ├── TableFormatter.cs             # Console table with auto-sizing
│   └── JsonFormatter.cs              # JSON serialization (no external libs)
│
├── Commands/                         # Command implementations
│   ├── SearchCommand.cs
│   ├── DownloadCommand.cs
│   ├── InstallCommand.cs
│   ├── UninstallCommand.cs
│   ├── HistoryCommand.cs
│   ├── ServicesCommand.cs
│   └── StatusCommand.cs
│
└── Properties/
    └── AssemblyInfo.cs               # Assembly metadata
```

### Design Principles

1. **Single Source of Truth (DRY):** `CliSpec.cs` defines all commands, options, defaults, and validation rules. Help text and validation are generated from this spec.

2. **Zero External Dependencies:** Uses only .NET Framework 4.5 BCL and WUApiLib COM interop. No NuGet packages.

3. **Fail-Safe Defaults:** Operations continue on partial failures. Exit code 1 indicates partial success.

4. **Explicit Over Implicit:** EULA acceptance and admin privileges must be explicitly acknowledged.

---

## WUApiLib Interfaces

SolviaWindowsUpdater uses the following Windows Update Agent COM interfaces:

| Interface | Purpose |
|-----------|---------|
| `IUpdateSession3` | Main session object; creates searchers, downloaders, installers |
| `IUpdateSearcher` | Search for updates, query history |
| `IUpdateDownloader` | Download update files |
| `IUpdateInstaller` | Install and uninstall updates |
| `IUpdateServiceManager2` | Manage registered update services |
| `ISystemInformation` | Query system status (reboot required) |
| `IUpdate` | Individual update properties |
| `IUpdateCollection` | Collection of updates for batch operations |
| `IUpdateHistoryEntry` | Historical update operation record |
| `IDownloadResult` / `IInstallationResult` | Operation results |

### COM Reference

The project references WUApiLib via COM interop:

```xml
<COMReference Include="WUApiLib">
  <Guid>{B596CC9F-56E5-419E-A622-E01BB457431E}</Guid>
  <VersionMajor>2</VersionMajor>
  <VersionMinor>0</VersionMinor>
  <EmbedInteropTypes>True</EmbedInteropTypes>
</COMReference>
```

---

## Automation & Scripting

### Scheduled Task Example

Create a scheduled task to install updates nightly:

```cmd
schtasks /create /tn "NightlyWindowsUpdate" /tr ^
    "C:\Tools\SolviaWindowsUpdater.exe install --all --accept-eulas --noreboot --log-path C:\Logs\wu-nightly.log" ^
    /sc daily /st 02:00 /ru SYSTEM
```

### PowerShell Wrapper

```powershell
function Invoke-WindowsUpdate {
    param(
        [switch]$DownloadOnly,
        [switch]$Install,
        [string]$Criteria = "IsInstalled=0"
    )

    $exe = "C:\Tools\SolviaWindowsUpdater.exe"

    if ($Install) {
        $output = & $exe install --all --accept-eulas --noreboot --criteria $Criteria --output json 2>&1
    } elseif ($DownloadOnly) {
        $output = & $exe download --all --criteria $Criteria --output json 2>&1
    } else {
        $output = & $exe search --criteria $Criteria --output json 2>&1
    }

    $exitCode = $LASTEXITCODE

    return @{
        ExitCode = $exitCode
        Updates = ($output | ConvertFrom-Json)
    }
}

# Usage
$result = Invoke-WindowsUpdate -Install
if ($result.ExitCode -eq 0) {
    Write-Host "Installed $($result.Updates.Count) updates"
}
```

### CI/CD Pipeline Example (Azure DevOps)

```yaml
- task: PowerShell@2
  displayName: 'Check for Windows Updates'
  inputs:
    targetType: 'inline'
    script: |
      $result = & SolviaWindowsUpdater.exe search --output json | ConvertFrom-Json
      Write-Host "Found $($result.Count) available updates"
      if ($result.Count -gt 0) {
        Write-Host "##vso[task.setvariable variable=UpdatesAvailable]true"
      }
```

### Parsing JSON Output

```powershell
# Get updates as objects
$updates = & SolviaWindowsUpdater.exe search --output json | ConvertFrom-Json

# Filter critical updates
$critical = $updates | Where-Object { $_.msrcSeverity -eq "Critical" }

# Get total download size
$totalSize = ($updates | Measure-Object -Property maxDownloadSize -Sum).Sum
Write-Host "Total download size: $([math]::Round($totalSize / 1MB, 2)) MB"
```

---

## Troubleshooting

### Common Issues

#### "Windows Update service is not running"

```cmd
:: Start the service
net start wuauserv

:: Or via PowerShell
Start-Service wuauserv
```

#### "Administrator privileges required"

Run the command from an elevated command prompt or PowerShell:

```cmd
:: CMD - Run as Administrator
runas /user:Administrator "cmd /k cd /d C:\Tools"

:: PowerShell - Start elevated
Start-Process powershell -Verb RunAs
```

#### "No updates found"

- Check your search criteria syntax
- Try `--server microsoftupdate` for broader coverage
- Use `--include-hidden` to see hidden updates
- Verify network connectivity to Windows Update servers

#### "EULA not accepted"

Add `--accept-eulas` to install commands:

```cmd
SolviaWindowsUpdater install --all --accept-eulas
```

#### "Update cannot be uninstalled"

Not all updates support uninstallation. Check `IsUninstallable` in search output:

```cmd
SolviaWindowsUpdater search --criteria "IsInstalled=1" --output json-full | findstr "isUninstallable"
```

### Debug Logging

Enable debug logging for troubleshooting:

```cmd
SolviaWindowsUpdater search --log-level Debug --log-path C:\Logs\wu-debug.log
```

### Common WUA Error Codes

| HRESULT | Code | Description |
|---------|------|-------------|
| 0x80240001 | WU_E_NO_SERVICE | Windows Update Agent not available |
| 0x80240016 | WU_E_INSTALL_NOT_ALLOWED | Another installation in progress |
| 0x80240017 | WU_E_NOT_APPLICABLE | No applicable updates |
| 0x8024001F | WU_E_NO_CONNECTION | Network connection unavailable |
| 0x80240022 | WU_E_ALL_UPDATES_FAILED | All update operations failed |
| 0x80240032 | WU_E_INVALID_CRITERIA | Invalid search criteria syntax |
| 0x80240034 | WU_E_DOWNLOAD_FAILED | Download failed |
| 0x80070005 | E_ACCESSDENIED | Access denied (run as admin) |

---

## Security Considerations

1. **Administrator Privileges:** Download, install, and uninstall operations require elevation. The tool checks and reports this requirement.

2. **EULA Acceptance:** The `--accept-eulas` flag explicitly acknowledges license acceptance. This is required for compliance.

3. **No Credential Storage:** The tool does not store or cache credentials.

4. **Audit Logging:** All operations are logged with timestamps for audit purposes.

5. **No Network Exfiltration:** The tool communicates only with Windows Update services (configured in Windows).

6. **WSUS Not Supported:** WSUS is deprecated and not implemented, reducing attack surface.

---

## Known Limitations

1. **No WSUS Support:** Windows Server Update Services configuration is not supported (deprecated).

2. **No Offline Scan Packages:** Offline cabinet file scanning is planned for v2.0.

3. **No Async Operations:** All operations are synchronous. Long downloads block the console.

4. **Single Session:** Only one instance should run at a time to avoid WUA conflicts.

5. **No Category Filtering in Selection:** The `--select` option works on KB or index, not categories.

6. **Console Only:** No GUI. Designed for scripting and automation.

---

## Roadmap

### v1.1 (Planned)
- [ ] `config` command to show WU configuration
- [ ] `hide` / `unhide` commands for managing hidden updates
- [ ] CSV output format
- [ ] Progress percentage in JSON output

### v2.0 (Future)
- [ ] Offline scan package support (`.cab` files)
- [ ] Async download with cancellation
- [ ] Category-based selection (`--select category:Security`)
- [ ] Update scheduling

---

## License

Copyright (c) Solvia. All rights reserved.

This software is proprietary. Unauthorized copying, distribution, or modification is prohibited.

---

## Support

For issues and feature requests, please open an issue on the GitHub repository.

For commercial support inquiries, contact: support@solvia.ch
