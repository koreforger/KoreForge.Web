# Versioning Guide

## Overview

This solution uses **Semantic Versioning 2.0.0** with Git tags as the single source of truth. We rely on [MinVer](https://github.com/adamralph/minver) (configured in `Directory.Build.props`) to compute the version during every build, pack, and publish. All packable projects within this solution share the exact same version for a given commit. Test and sample projects inherit the configuration but remain non-packable.

### Configuration Summary

| Setting | Value |
| --- | --- |
| Tag Prefix | `KoreForge.Web/v` |
| Auto Increment | `minor` |
| Default Pre-release | `alpha.0` |

Example release tag: `KoreForge.Web/v1.4.0`

## Versioning Scripts

This solution provides automation scripts in the `scr/` folder to manage versions:

### git-push-nuget.ps1

Creates a release tag and pushes it to trigger NuGet publication:

```powershell
.\scr\git-push-nuget.ps1 -Version 1.2.0 -Note "Add new feature"
```

This script:
- Stages and commits any pending working-tree changes
- Creates an annotated tag `KoreForge.Web/v<Version>`
- Pushes the tag to origin to trigger the `publish-nuget.yml` CI workflow

## Semantic Versioning Rules

- **MAJOR** (`X.y.z`): Breaking changes in public API or behavior.
  - Examples: removing or renaming a public type, changing method signatures, altering behavior in a way that breaks existing consumers.
- **MINOR** (`x.Y.z`): Backwards-compatible feature additions.
  - Examples: adding new options, methods, events, or features that do not break existing code.
- **PATCH** (`x.y.Z`): Backwards-compatible fixes and improvements.
  - Examples: bug fixes, performance tuning, documentation updates, internal refactors without API changes.

## Release Workflow

1. Ensure the working tree is clean and all tests pass:
   ```powershell
   .\scr\build-test-codecoverage.ps1
   ```

2. Decide the new SemVer (MAJOR.MINOR.PATCH) according to the rules above.

3. Create and push the release tag:
   ```powershell
   .\scr\git-push-nuget.ps1 -Version 1.2.0 -Note "Short description"
   ```

4. The CI `publish-nuget.yml` workflow will build, test, pack, and push all three packages to NuGet automatically.

## Pre-release and Development Builds

- Commits after the latest tag automatically produce pre-release versions such as `1.3.0-alpha.0.1`, `1.3.0-alpha.0.2`, etc.
- These builds are suitable for internal consumption, previews, or testing feeds but should not be published as official releases.
- To publish a preview release, use a pre-release tag like `1.4.0-beta.1`:
  ```powershell
  .\scr\git-push-nuget.ps1 -Version 1.4.0-beta.1 -Note "Preview release"
  ```

## Do's and Don'ts

**Do:**
- ✅ Run `build-test-codecoverage.ps1` to verify tests and coverage before releasing
- ✅ Use `git-push-nuget.ps1` to create version tags
- ✅ Follow the SemVer rules when choosing MAJOR vs MINOR vs PATCH
- ✅ Ensure tags are pushed to origin so CI sees the same version

**Don't:**
- ❌ Manually edit `<Version>`, `<PackageVersion>`, etc. in project files
- ❌ Create tags that don't follow the `{ProductName}/vX.Y.Z` pattern
- ❌ Forget to push tags to origin

## Cheat Sheet

| Scenario | Command |
| --- | --- |
| Check current version | `git describe --tags --match 'KoreForge.Web/v*'` |
| Breaking change release | `.\scr\git-push-nuget.ps1 -Version 2.0.0 -Note "..."` |
| New feature release | `.\scr\git-push-nuget.ps1 -Version 1.3.0 -Note "..."` |
| Bug fix / patch release | `.\scr\git-push-nuget.ps1 -Version 1.2.1 -Note "..."` |
| Preview/beta release | `.\scr\git-push-nuget.ps1 -Version 1.4.0-beta.1 -Note "..."` |

## Relation to Other KoreForge Libraries

All KoreForge.* repositories follow this same versioning pattern:
- Each solution has its own tag prefix (e.g., `KoreForge.Logging/v`, `KoreForge.Time/v`)
- Each solution maintains its own version and release cadence
- Cross-solution dependencies use standard NuGet package references

## Technical Details

MinVer configuration in `Directory.Build.props`:

```xml
<PropertyGroup>
  <MinVerTagPrefix>KoreForge.Web/v</MinVerTagPrefix>
  <MinVerAutoIncrement>minor</MinVerAutoIncrement>
  <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

This ensures:
- `Version`, `PackageVersion`, `AssemblyVersion`, and `FileVersion` are all derived from Git tags
- Consistent versioning across all packable projects in the solution
