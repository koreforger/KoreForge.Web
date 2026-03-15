# Versioning Guide

KhaosKode.Web.Authorization uses [MinVer](https://github.com/adamralph/minver) to infer semantic versions directly from Git history. No manual edits to `.csproj` files are required.

## Version Source of Truth  

* Tags must follow the pattern `KhaosKode.Web.Authorization/v<semver>` (example: `KhaosKode.Web.Authorization/v1.2.0`).
* MinVer walks back through the current commit ancestry looking for the highest matching tag and derives pre-release versions when no tag is present.

## Pre-Release Strategy

* `Directory.Build.props` sets `MinVerAutoIncrement` to `minor`. When building on top of the latest `vX.Y.Z` tag, MinVer increments the **minor** portion and appends the pre-release identifiers `alpha.0.<build>`. Example output: `1.4.0-alpha.0.5`.
* CI builds from feature branches naturally produce `alpha` versions, making it clear the artifacts are not yet stable.

## Releasing

1. Ensure `main` (or the release branch) contains the desired commit.
2. Create the annotated tag, e.g.

   ```powershell
   git tag -a KhaosKode.Web.Authorization/v1.5.0 -m "Release 1.5.0"
   git push origin KhaosKode.Web.Authorization/v1.5.0
   ```

3. Run `dotnet pack -c Release` (locally or in CI). Because a matching tag exists, MinVer emits `1.5.0` with no pre-release suffix and the package lands in `artifacts/packages`.

## Branching Expectations

* `main` — always releasable. Tags are applied here.
* `feature/*` — short-lived feature branches. Builds remain `alpha` until merged into `main` and tagged.
* `release/*` (optional) — if you need validation before tagging, create a release branch, run QA, then tag once approved.

## Package Contents

Every packable project inherits shared metadata:

* README + LICENSE at the package root.
* All files under `docs/` published to `buildTransitive/docs/` so downstream consumers can view the specification, developer guide, user guide, and this document.
* PDBs are emitted into `.snupkg` symbol packages.

Keep tags in sync with published NuGet versions to prevent version drift. If a tag is created in error, delete it locally and remotely (`git tag -d ...`, `git push origin :refs/tags/...`) so MinVer does not pick it up on future builds.
