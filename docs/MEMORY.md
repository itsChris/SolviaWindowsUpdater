# Project Memory: SolviaWindowsUpdater

This document captures important context, decisions, and knowledge about the project for future reference.

---

## Project Overview

**SolviaWindowsUpdater** is a Windows Update management CLI tool that wraps the native WUApiLib COM API.

| Attribute | Value |
|-----------|-------|
| Language | C# |
| Framework | .NET Framework 4.5 |
| API | WUApiLib COM Interop |
| Type | Console Application |
| Dependencies | Zero (only BCL + WUApiLib) |

---

## Key Design Decisions

### D001: .NET Framework 4.5 Target

**Decision:** Target .NET Framework 4.5 instead of .NET Core/6+

**Rationale:**
- Maximum compatibility with Windows 7 SP1
- WUApiLib COM interop works seamlessly
- No runtime installation required on modern Windows
- Simpler deployment (single .exe)

**Trade-offs:**
- No cross-platform support (acceptable - WUA is Windows-only)
- Older C# language version (7.3)

---

### D002: Zero External Dependencies

**Decision:** Use no NuGet packages or external libraries

**Rationale:**
- Simplest possible deployment
- No version conflicts
- No supply chain risks
- Smaller binary size

**Implementations:**
- Custom argument parser (no System.CommandLine)
- Manual JSON serialization (no Newtonsoft.Json)
- Custom table formatter (no Spectre.Console)
- Custom logging (no Serilog/NLog)

---

### D003: Single Source of Truth (CliSpec)

**Decision:** Define all CLI commands, options, and validation rules in a single `CliSpec.cs` file

**Rationale:**
- DRY principle - no duplication between help text and validation
- Easy to maintain and extend
- Consistent behavior guaranteed
- Self-documenting code

**Implementation:**
- `CliSpec.Commands` - All command definitions
- `CliSpec.GlobalOptions` - Global option definitions
- `CliSpec.ValidationRules` - All validation rules
- `HelpGenerator` reads from CliSpec
- `Validator` reads from CliSpec

---

### D004: Implicit Search for Download/Install

**Decision:** Download and Install commands perform an implicit search before operation

**Rationale:**
- Simpler user workflow (one command instead of two)
- Updates are always fresh from server
- Selection syntax works on live data

**Trade-offs:**
- Slightly slower operations (search overhead)
- Cannot use cached search results

---

### D005: All History by Default

**Decision:** History command shows all entries by default (not paginated)

**Rationale:**
- User explicitly requested "list ALL updates"
- Most useful for compliance and auditing
- Pagination still available via `--count`

**Implementation:**
- Call `GetTotalHistoryCount()` when count not specified
- Pass total count to `GetHistory()`

---

### D006: Exit Code Strategy

**Decision:** Use distinct exit codes (0-10) for different failure scenarios

**Rationale:**
- Enables precise scripting and automation
- Clear differentiation between error types
- Exit code 1 for partial success (some ops failed, some succeeded)

**Codes:**
```
0  = Success
1  = Partial Success
2  = Validation Error
3  = Elevation Required
4  = No Updates Found
5  = WUA Error
6  = EULA Not Accepted
7  = Update Not Ready
8  = Update Not Uninstallable
9  = User Cancelled
10 = Unexpected Error
```

---

### D007: Reboot Handling

**Decision:** Default behavior prompts for reboot; `--noreboot` suppresses prompt

**Rationale:**
- Safe default - user is always informed
- Automation scenarios need `--noreboot`
- Tool reports reboot-required status regardless

---

## Technical Notes

### WUApiLib COM Interop

The project uses the Windows Update Agent API via COM:

```xml
<COMReference Include="WUApiLib">
  <Guid>{B596CC9F-56E5-419E-A622-E01BB457431E}</Guid>
  <VersionMajor>2</VersionMajor>
  <VersionMinor>0</VersionMinor>
  <EmbedInteropTypes>True</EmbedInteropTypes>
</COMReference>
```

**Key Interfaces:**
- `IUpdateSession3` - Create searcher, downloader, installer
- `IUpdateSearcher` - Search and history
- `IUpdateDownloader` - Download with progress
- `IUpdateInstaller` - Install/uninstall
- `IUpdateServiceManager2` - Service management
- `ISystemInformation` - Reboot status

**Important:** Use concrete `UpdateCollection` class, not `IUpdateCollection` interface, for `Updates` property assignment.

---

### Search Criteria Syntax

WUA uses a SQL-like criteria language:

```
IsInstalled=0                    # Not installed
IsInstalled=0 AND BrowseOnly=1   # Optional updates
Type='Software'                  # Software only
CategoryIDs contains 'GUID'      # By category
```

**Common Category GUIDs:**
- `0FA1201D-4330-4FA8-8AE9-B877473B6441` - Security Updates
- `E6CF1350-C01B-414D-A61F-263D14D133B4` - Critical Updates
- `CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83` - Updates
- `3689BDC8-B205-4AF4-8D4A-A63924C5E9D5` - Drivers

---

### Server Modes

| Mode | Service ID | Description |
|------|------------|-------------|
| windowsupdate | (default) | OS updates only |
| microsoftupdate | 7971f918-a847-4430-9279-4a52d1efe18d | Includes Office, drivers |

---

## Common Issues & Solutions

### Issue: "Cannot implicitly convert IUpdateCollection to UpdateCollection"

**Cause:** COM interop returns interface type but properties expect concrete type

**Solution:** Return `UpdateCollection` from `CreateUpdateCollection()`:
```csharp
private UpdateCollection CreateUpdateCollection()
{
    return new UpdateCollection();
}
```

---

### Issue: MSBuild command not found

**Cause:** MSBuild not in PATH

**Solution:** Use full path:
```cmd
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
```

---

### Issue: "Windows Update service is not running"

**Cause:** wuauserv service stopped

**Solution:**
```cmd
net start wuauserv
```

---

## File Structure

```
SolviaWindowsUpdater/
├── Cli/
│   ├── CliSpec.cs          # Single source of truth
│   ├── ArgParser.cs        # Custom argument parser
│   ├── HelpGenerator.cs    # Help from CliSpec
│   ├── Validator.cs        # Validation from CliSpec
│   └── ParsedArgs.cs       # Parsed argument container
├── Core/
│   ├── Logger.cs           # Console + file logging
│   ├── ExitCodes.cs        # Exit code constants
│   └── AdminHelper.cs      # Admin privilege checking
├── Wua/
│   ├── WuaClient.cs        # Main API wrapper (~860 lines)
│   ├── UpdateInfo.cs       # Update data model
│   ├── HistoryEntry.cs     # History entry model
│   ├── ServiceInfo.cs      # Service info model
│   └── ProgressReporter.cs # Progress reporting
├── Output/
│   ├── TableFormatter.cs   # Console table formatting
│   └── JsonFormatter.cs    # JSON serialization
├── Commands/
│   ├── SearchCommand.cs
│   ├── DownloadCommand.cs
│   ├── InstallCommand.cs
│   ├── UninstallCommand.cs
│   ├── HistoryCommand.cs
│   ├── ServicesCommand.cs
│   └── StatusCommand.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Program.cs              # Entry point
├── App.config
├── SolviaWindowsUpdater.csproj
├── SolviaWindowsUpdater.sln
├── README.md
├── LICENSE
└── docs/
    ├── PRD.md
    ├── BACKLOG.md
    └── MEMORY.md
```

---

## Testing Notes

### Manual Test Scenarios

1. **Search:** `search --max-results 5`
2. **Search JSON:** `search --output json`
3. **History:** `history` (all entries)
4. **History limited:** `history --count 10`
5. **Status:** `status`
6. **Services:** `services list`
7. **Help:** `--help`, `help search`
8. **Version:** `version`

### Admin-Required Operations

- Download: `download --all` (as admin)
- Install: `install --all --accept-eulas` (as admin)
- Uninstall: `uninstall --select kb:KBxxxx` (as admin)
- Services remove: `services remove --service-id GUID` (as admin)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-17 | Initial release |

---

## Future Considerations

1. **Config command** - Display WU configuration (IAutomaticUpdates, registry)
2. **Hide/Unhide** - Manage hidden updates
3. **CSV output** - Spreadsheet compatibility
4. **Offline scan** - Support for .cab packages
5. **Unit tests** - Add test coverage

---

## Contact

- **Repository:** https://github.com/itsChris/SolviaWindowsUpdater
- **Support:** support@solvia.ch
