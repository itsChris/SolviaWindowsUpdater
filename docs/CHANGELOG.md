# Changelog

All notable changes to SolviaWindowsUpdater will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- `config` command to display Windows Update configuration
- `hide` and `unhide` commands for managing hidden updates
- CSV output format
- Progress percentage in JSON output

---

## [1.0.0] - 2025-01-17

### Added

#### Commands
- **search**: Search for available Windows updates with WUA criteria syntax
  - `--criteria` for custom search expressions
  - `--include-hidden` to include hidden updates
  - `--max-results` to limit results
  - `--output` for table/json/json-full formats
  - `--server` for Windows Update or Microsoft Update

- **download**: Download selected updates to local cache
  - `--all` to download all available updates
  - `--select kb:KBxxxx` or `--select index:1,2,3` for specific updates
  - `--force` to re-download cached updates
  - `--whatif` for dry-run mode

- **install**: Install selected updates
  - `--accept-eulas` for automatic EULA acceptance
  - `--noreboot` to suppress reboot prompt
  - Automatic download if not cached
  - Progress reporting

- **uninstall**: Uninstall previously installed updates
  - Check for uninstallability
  - `--force` to attempt uninstall regardless
  - `--noreboot` to suppress reboot prompt

- **history**: View Windows Update installation history
  - Shows all entries by default
  - `--count` to limit entries
  - `--start-index` for pagination
  - Multiple output formats

- **services**: Manage Windows Update services
  - `list` subcommand to show registered services
  - `remove` subcommand to remove a service

- **status**: Check system reboot status
  - Reports if reboot is required

- **help**: Show help information
  - General help or command-specific help

- **version**: Show version information

#### Features
- Zero external dependencies (only .NET Framework 4.5 BCL + WUApiLib)
- Single source of truth CLI specification (CliSpec.cs)
- Comprehensive validation with 21 rules
- Three output formats: table, json, json-full
- Dual logging (console + file)
- Configurable log levels (Trace, Debug, Info, Warn, Error)
- Real-time progress reporting for downloads/installs
- Partial failure handling (continue on individual failures)
- Distinct exit codes (0-10) for scripting

#### Documentation
- Comprehensive README.md with examples
- Product Requirements Document (PRD.md)
- Product Backlog (BACKLOG.md)
- Project Memory (MEMORY.md)
- Contributing Guidelines (CONTRIBUTING.md)
- MIT License

### Technical
- .NET Framework 4.5 target for Windows 7+ compatibility
- WUApiLib COM interop for Windows Update Agent API
- Custom argument parser (no external libraries)
- Custom JSON serializer (no external libraries)
- Custom table formatter with auto-sizing columns

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0 | 2025-01-17 | Initial release with full feature set |

---

## Upgrade Notes

### Upgrading to 1.0.0

This is the initial release. No upgrade path required.

---

## Release Checklist

For maintainers releasing new versions:

1. [ ] Update version in `Cli/CliSpec.cs`
2. [ ] Update version in `Properties/AssemblyInfo.cs`
3. [ ] Update CHANGELOG.md with release notes
4. [ ] Build in Release configuration
5. [ ] Test core functionality
6. [ ] Create GitHub release with binary
7. [ ] Tag release in git
