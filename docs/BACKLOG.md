# Product Backlog: SolviaWindowsUpdater

## Backlog Overview

| Priority | Count | Status |
|----------|-------|--------|
| P0 (Critical) | 0 | All shipped in v1.0 |
| P1 (High) | 4 | Planned for v1.1 |
| P2 (Medium) | 6 | Planned for v2.0 |
| P3 (Low) | 5 | Future consideration |

---

## Version 1.0.0 (Released)

All items completed and shipped.

### Shipped Features

| ID | Feature | Status |
|----|---------|--------|
| SWUA-001 | Search command with WUA criteria | ✅ Done |
| SWUA-002 | Download command with selection | ✅ Done |
| SWUA-003 | Install command with EULA handling | ✅ Done |
| SWUA-004 | Uninstall command | ✅ Done |
| SWUA-005 | History command (all entries) | ✅ Done |
| SWUA-006 | Services list/remove commands | ✅ Done |
| SWUA-007 | Status command (reboot check) | ✅ Done |
| SWUA-008 | JSON output (minimal + full) | ✅ Done |
| SWUA-009 | Table output (auto-sized) | ✅ Done |
| SWUA-010 | Comprehensive logging | ✅ Done |
| SWUA-011 | Exit codes (11 defined) | ✅ Done |
| SWUA-012 | Help system from CliSpec | ✅ Done |
| SWUA-013 | Validation engine | ✅ Done |
| SWUA-014 | Progress reporting | ✅ Done |
| SWUA-015 | Dry-run mode (--whatif) | ✅ Done |
| SWUA-016 | Reboot control (--noreboot) | ✅ Done |
| SWUA-017 | Server modes (WU/MU) | ✅ Done |

---

## Version 1.1 (Planned)

### P1 - High Priority

| ID | Feature | Description | Effort |
|----|---------|-------------|--------|
| SWUA-101 | Config command | Display Windows Update configuration from registry and IAutomaticUpdates | M |
| SWUA-102 | Hide command | Hide updates from search results | S |
| SWUA-103 | Unhide command | Unhide previously hidden updates | S |
| SWUA-104 | CSV output format | Add `--output csv` for spreadsheet compatibility | S |

### P2 - Medium Priority

| ID | Feature | Description | Effort |
|----|---------|-------------|--------|
| SWUA-105 | Progress in JSON | Include progress percentage in JSON streaming output | S |
| SWUA-106 | Quiet mode | Add `--quiet` flag to suppress non-essential output | S |

---

## Version 2.0 (Future)

### P2 - Medium Priority

| ID | Feature | Description | Effort |
|----|---------|-------------|--------|
| SWUA-201 | Offline scan packages | Support scanning with offline .cab files | L |
| SWUA-202 | Async downloads | Non-blocking download with cancellation | L |
| SWUA-203 | Category selection | `--select category:Security` syntax | M |
| SWUA-204 | Batch operations | Process multiple commands from file | M |

### P3 - Low Priority

| ID | Feature | Description | Effort |
|----|---------|-------------|--------|
| SWUA-301 | PowerShell module wrapper | PS module wrapping the CLI | L |
| SWUA-302 | Scheduled operations | Built-in scheduling without Task Scheduler | L |
| SWUA-303 | Remote execution | Execute on remote machines via WinRM | XL |
| SWUA-304 | HTML report | Generate HTML update reports | M |
| SWUA-305 | Email notifications | Send email on completion | M |

---

## Backlog Items (Detailed)

### SWUA-101: Config Command

**Priority:** P1
**Effort:** Medium
**Version:** 1.1

**Description:**
Add a `config` command to display Windows Update configuration settings.

**Acceptance Criteria:**
- [ ] Show Automatic Updates settings (IAutomaticUpdates2)
- [ ] Show registry policy settings (HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate)
- [ ] Show registered services (reuse existing)
- [ ] Support `--output json` format
- [ ] Handle missing registry keys gracefully

**Technical Notes:**
- Use `IAutomaticUpdates2` for AU settings
- Read registry for policy settings
- No admin required for read-only

---

### SWUA-102: Hide Command

**Priority:** P1
**Effort:** Small
**Version:** 1.1

**Description:**
Add ability to hide updates so they don't appear in search results.

**Acceptance Criteria:**
- [ ] `hide --select kb:KBxxxx` hides specified updates
- [ ] Requires admin privileges
- [ ] Confirmation before hiding
- [ ] Support `--whatif` for preview
- [ ] Log all hide operations

**Technical Notes:**
- Use `IUpdate.IsHidden = true`
- Requires search first to get IUpdate reference

---

### SWUA-103: Unhide Command

**Priority:** P1
**Effort:** Small
**Version:** 1.1

**Description:**
Add ability to unhide previously hidden updates.

**Acceptance Criteria:**
- [ ] `unhide --select kb:KBxxxx` unhides specified updates
- [ ] `unhide --all` unhides all hidden updates
- [ ] List hidden updates with `search --include-hidden`
- [ ] Requires admin privileges
- [ ] Support `--whatif` for preview

**Technical Notes:**
- Use `IUpdate.IsHidden = false`
- Search with `IsHidden=1` to find hidden updates

---

### SWUA-104: CSV Output Format

**Priority:** P1
**Effort:** Small
**Version:** 1.1

**Description:**
Add CSV output format for spreadsheet compatibility.

**Acceptance Criteria:**
- [ ] `--output csv` produces valid CSV
- [ ] Proper escaping of commas, quotes
- [ ] Header row with column names
- [ ] Consistent column order
- [ ] Works for search and history commands

**Technical Notes:**
- Add `CsvFormatter.cs` in Output/
- Follow RFC 4180 for CSV format

---

### SWUA-201: Offline Scan Packages

**Priority:** P2
**Effort:** Large
**Version:** 2.0

**Description:**
Support offline scanning with Windows Update cabinet files (.cab).

**Acceptance Criteria:**
- [ ] `search --offline-scan <path.cab>` uses offline package
- [ ] Download offline packages from Microsoft Catalog
- [ ] Handle package extraction
- [ ] Validate package signatures
- [ ] Error handling for corrupt packages

**Technical Notes:**
- Use `IUpdateServiceManager::AddScanPackageService`
- Requires downloading wsusscn2.cab or similar
- Consider storage of extracted packages

---

### SWUA-202: Async Downloads

**Priority:** P2
**Effort:** Large
**Version:** 2.0

**Description:**
Implement non-blocking downloads with progress streaming and cancellation.

**Acceptance Criteria:**
- [ ] Downloads run asynchronously
- [ ] Progress streamed to console
- [ ] Ctrl+C cancels gracefully
- [ ] Resume partial downloads
- [ ] Multiple concurrent downloads (optional)

**Technical Notes:**
- Use `IDownloadJob` instead of synchronous `Download()`
- Implement proper cancellation token handling
- Handle COM callback interfaces

---

### SWUA-203: Category Selection

**Priority:** P2
**Effort:** Medium
**Version:** 2.0

**Description:**
Allow selecting updates by category name instead of just KB or index.

**Acceptance Criteria:**
- [ ] `--select category:Security` selects security updates
- [ ] `--select category:Critical,Drivers` selects multiple
- [ ] Case-insensitive matching
- [ ] Show available categories in help
- [ ] Combine with KB selection

**Technical Notes:**
- Map category names to GUIDs
- Use `IUpdate.Categories` for matching
- Define canonical category names

---

## Effort Sizing Guide

| Size | Description | Typical Duration |
|------|-------------|------------------|
| S | Small - Simple feature, < 100 LOC | 1-2 hours |
| M | Medium - Moderate complexity, 100-500 LOC | 4-8 hours |
| L | Large - Significant feature, 500-1000 LOC | 1-2 days |
| XL | Extra Large - Major feature, > 1000 LOC | 3-5 days |

---

## Bug Backlog

No known bugs at this time.

| ID | Description | Severity | Status |
|----|-------------|----------|--------|
| - | - | - | - |

---

## Technical Debt

| ID | Description | Priority | Effort |
|----|-------------|----------|--------|
| TD-001 | Add unit tests | P2 | L |
| TD-002 | Add integration tests | P3 | L |
| TD-003 | Improve error messages for edge cases | P3 | M |
| TD-004 | Refactor WuaClient for testability | P3 | M |

---

## Rejected/Deferred Items

| ID | Feature | Reason |
|----|---------|--------|
| REJ-001 | WSUS support | Deprecated technology |
| REJ-002 | GUI interface | Out of scope - CLI focus |
| REJ-003 | Linux/macOS | Platform not supported by WUA |
| DEF-001 | .NET Core port | Requires COM interop work |

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-01-17 | 1.0.0 | Initial release with all core features |
