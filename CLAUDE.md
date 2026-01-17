# CLAUDE.md - Project Context for Claude Code

## Project Overview

**SolviaWindowsUpdater** is a Windows Update Agent CLI tool built on .NET Framework 4.5 using WUApiLib COM Interop.

## Quick Reference

| Item | Value |
|------|-------|
| Language | C# (.NET Framework 4.5) |
| Build | `msbuild SolviaWindowsUpdater.sln /p:Configuration=Release` |
| Output | `bin\Release\SolviaWindowsUpdater.exe` |
| Dependencies | None (only BCL + WUApiLib COM) |

## Build Commands

```cmd
# Using Visual Studio 2022 MSBuild
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SolviaWindowsUpdater.sln /p:Configuration=Release

# Using .NET Framework MSBuild
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe SolviaWindowsUpdater.sln /p:Configuration=Release
```

## Architecture

```
SolviaWindowsUpdater/
├── Cli/                    # CLI layer (CliSpec is single source of truth)
│   ├── CliSpec.cs          # Commands, options, validation rules
│   ├── ArgParser.cs        # Argument parsing
│   ├── HelpGenerator.cs    # Help text generation
│   ├── Validator.cs        # Validation engine
│   └── ParsedArgs.cs       # Parsed args container
├── Core/                   # Infrastructure
│   ├── Logger.cs           # Logging (console + file)
│   ├── ExitCodes.cs        # Exit code constants (0-10)
│   └── AdminHelper.cs      # Admin privilege detection
├── Wua/                    # Windows Update Agent wrapper
│   ├── WuaClient.cs        # Main API client
│   ├── UpdateInfo.cs       # Update model
│   ├── HistoryEntry.cs     # History model
│   ├── ServiceInfo.cs      # Service model
│   └── ProgressReporter.cs # Progress reporting
├── Output/                 # Formatters
│   ├── TableFormatter.cs   # Console tables
│   └── JsonFormatter.cs    # JSON output
├── Commands/               # Command implementations
│   ├── SearchCommand.cs
│   ├── DownloadCommand.cs
│   ├── InstallCommand.cs
│   ├── UninstallCommand.cs
│   ├── HistoryCommand.cs
│   ├── ServicesCommand.cs
│   └── StatusCommand.cs
└── Program.cs              # Entry point
```

## Key Design Patterns

1. **Single Source of Truth**: `CliSpec.cs` defines all commands, options, defaults, and validation rules. Help and validation are generated from this.

2. **Zero Dependencies**: No NuGet packages. Custom implementations for:
   - Argument parsing (ArgParser.cs)
   - JSON serialization (JsonFormatter.cs)
   - Table formatting (TableFormatter.cs)
   - Logging (Logger.cs)

3. **COM Interop**: Uses WUApiLib directly via COM reference:
   ```xml
   <COMReference Include="WUApiLib">
     <Guid>{B596CC9F-56E5-419E-A622-E01BB457431E}</Guid>
   </COMReference>
   ```

## Commands

| Command | Description | Admin |
|---------|-------------|-------|
| search | Search for updates | No |
| download | Download updates | Yes |
| install | Install updates | Yes |
| uninstall | Uninstall updates | Yes |
| history | View update history | No |
| services | List/remove services | Varies |
| status | Check reboot status | No |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Partial success |
| 2 | Validation error |
| 3 | Elevation required |
| 4 | No updates found |
| 5 | WUA error |
| 6 | EULA not accepted |
| 7 | Update not ready |
| 8 | Cannot uninstall |
| 9 | User cancelled |
| 10 | Unexpected error |

## Common Tasks

### Add a new command option

1. Add to `CliSpec.cs` in the appropriate command's `Options` list
2. Add validation rule if needed in `ValidationRules`
3. Read option in command via `args.GetOption<T>("option-name")`

### Add a new command

1. Add command definition to `CliSpec.Commands`
2. Create `Commands/NewCommand.cs` with `Execute(ParsedArgs, WuaClient)`
3. Add case to switch in `Program.ExecuteCommand()`

### Add a new output format

1. Add format to `OutputFormat` enum in `CliSpec.cs`
2. Add formatter method in `Output/` folder
3. Update command to call formatter based on `args.GetOutputFormat()`

## Important Notes

- **UpdateCollection**: Use concrete `UpdateCollection` class, not `IUpdateCollection` interface, when assigning to `IUpdateDownloader.Updates` or `IUpdateInstaller.Updates`

- **History default**: History shows ALL entries by default (calls `GetTotalHistoryCount()`)

- **Implicit search**: Download/Install commands perform implicit search before operation

- **WUApiLib interfaces used**:
  - IUpdateSession3
  - IUpdateSearcher
  - IUpdateDownloader
  - IUpdateInstaller
  - IUpdateServiceManager2
  - ISystemInformation

## Testing

```cmd
# Basic tests
SolviaWindowsUpdater --help
SolviaWindowsUpdater version
SolviaWindowsUpdater status
SolviaWindowsUpdater search --max-results 5
SolviaWindowsUpdater history --count 10
SolviaWindowsUpdater services list

# Admin required
SolviaWindowsUpdater download --all --whatif
SolviaWindowsUpdater install --all --accept-eulas --whatif
```

## Documentation

- `README.md` - Comprehensive user documentation
- `docs/PRD.md` - Product requirements
- `docs/BACKLOG.md` - Feature backlog
- `docs/MEMORY.md` - Project context and decisions
