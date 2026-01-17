# Contributing to SolviaWindowsUpdater

Thank you for your interest in contributing to SolviaWindowsUpdater!

## Getting Started

### Prerequisites

- Windows 10/11 or Windows Server 2016+
- Visual Studio 2019/2022 or MSBuild
- .NET Framework 4.5+ SDK
- Git

### Building from Source

```cmd
git clone https://github.com/itsChris/SolviaWindowsUpdater.git
cd SolviaWindowsUpdater

# Build with MSBuild
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
    SolviaWindowsUpdater.sln /p:Configuration=Release
```

### Running Tests

```cmd
# Manual testing
bin\Release\SolviaWindowsUpdater.exe --help
bin\Release\SolviaWindowsUpdater.exe search --max-results 5
bin\Release\SolviaWindowsUpdater.exe status
```

## Code Style

### General Guidelines

- Use C# 7.3 syntax (maximum for .NET Framework 4.5 target)
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Keep methods focused and under 50 lines when possible
- Add XML documentation for public APIs

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `UpdateInfo` |
| Methods | PascalCase | `GetHistory()` |
| Properties | PascalCase | `IsDownloaded` |
| Private fields | _camelCase | `_searcher` |
| Local variables | camelCase | `updateCount` |
| Constants | PascalCase | `DefaultLogPath` |

### File Organization

```
Feature/
├── FeatureName.cs        # Main implementation
└── FeatureHelper.cs      # Helper classes (if needed)
```

## Architecture Guidelines

### Zero Dependencies Rule

This project maintains a strict zero-external-dependencies policy:

- ✅ .NET Framework BCL classes
- ✅ WUApiLib COM interop
- ❌ NuGet packages
- ❌ Third-party libraries

If you need functionality typically provided by a library, implement it manually in the appropriate location.

### Single Source of Truth

All CLI specifications are defined in `Cli/CliSpec.cs`:

- Commands
- Options (global and command-specific)
- Validation rules
- Default values

**Never** duplicate this information elsewhere. HelpGenerator and Validator read from CliSpec.

### Adding a New Command

1. **Define in CliSpec.cs:**
```csharp
new CommandDef
{
    Name = "mycommand",
    Description = "Does something useful",
    RequiresAdmin = false,
    Options = new List<OptionDef>
    {
        // command-specific options
    }
}
```

2. **Create Command class:**
```csharp
// Commands/MyCommand.cs
public static class MyCommand
{
    public static int Execute(ParsedArgs args, WuaClient client)
    {
        // Implementation
        return ExitCodes.Success;
    }
}
```

3. **Register in Program.cs:**
```csharp
case "mycommand":
    return MyCommand.Execute(args, client);
```

### Adding a New Option

1. **Define in CliSpec.cs** (global or command-specific)
2. **Add validation rule** if needed
3. **Read in command:** `args.GetOption<T>("option-name")`

## Pull Request Process

### Before Submitting

1. **Build successfully** with no warnings
2. **Test manually** with common scenarios
3. **Update documentation** if adding features
4. **Follow commit message format**

### Commit Message Format

```
<type>: <short description>

<optional longer description>

<optional footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding or updating tests
- `chore`: Build process or auxiliary tool changes

**Examples:**
```
feat: Add hide/unhide commands for updates

Implements the ability to hide updates from search results
and unhide previously hidden updates.

Closes #123
```

```
fix: Handle empty search results gracefully

Previously, an empty search result would throw a null reference
exception. Now returns exit code 4 (NoUpdatesFound).
```

### PR Checklist

- [ ] Code builds without warnings
- [ ] Tested on Windows 10/11
- [ ] Updated README.md if adding user-facing features
- [ ] Updated CLAUDE.md if changing architecture
- [ ] Added entry to BACKLOG.md if applicable
- [ ] Commit messages follow format

## Reporting Issues

### Bug Reports

Include:
1. Windows version
2. .NET Framework version
3. Command that failed
4. Full error output
5. Expected behavior
6. Log file content (if available)

### Feature Requests

Include:
1. Use case description
2. Proposed syntax/interface
3. Expected behavior
4. Any alternatives considered

## Code of Conduct

- Be respectful and constructive
- Focus on the code, not the person
- Welcome newcomers
- Assume good intentions

## Questions?

Open an issue with the "question" label or contact support@solvia.ch.
