# 🏁 Release Workflow Guide

This guide explains the release process for GhJSON.NET, which follows the `dev` → `main` flow.

## Overview

The release workflow allows you to:

- Release planned features and improvements from the `dev` branch
- Automatically prepare release documentation and version updates
- Create a structured PR flow: `dev` → `main`
- Build and publish NuGet packages via GitHub Releases

## Branch Strategy

| Branch | Purpose |
| ------ | ------- |
| `dev` | Active devment, feature integration |
| `main` | Stable releases only |
| `release/*` | Temporary release preparation branches |

## Workflow Steps

### Step 1: Develop Features in Develop Branch

1. Work on features, fixes, and improvements in the `dev` branch
2. Create PRs to `dev` for each feature/fix
3. Associate PRs and issues with a milestone (e.g., `1.2.0`)
4. Ensure all changes update `CHANGELOG.md` under `[Unreleased]`

### Step 2: Update Version in Directory.Build.props

1. Update the `<Version>` in `Directory.Build.props` to the target release version
2. Push to `dev` — this triggers the **🔄 Update Version Badge** workflow

### Step 3: Create PR from Develop to Main

1. Create a PR from `dev` to `main`
2. This triggers the following automated checks:
   - **🔨 CI Build & Test** — Builds and runs tests
   - **📝 Header Check** — Validates license headers
   - **🚫 Block Dev Release** — Blocks `-dev` versions from merging to main
   - **🔄 Prepare Release for Main** — Strips date suffixes, updates badges and changelog

### Step 4: Review and Merge

1. Review the PR and ensure all checks pass
2. The prepare-release workflow will:
   - Strip any date suffix from the version (e.g., `1.2.0-dev.250101` → `1.2.0-dev`)
   - Update README badges to match the version
   - Create a release section in CHANGELOG.md (moves `[Unreleased]` → `[X.Y.Z]`)
3. Merge the PR to `main`

### Step 5: Close Milestone (Optional)

1. Close the milestone associated with the release
2. This triggers:
   - **📝 Normalize Headers** — Creates a PR to normalize license headers
   - **Create Release Draft** — Creates a draft GitHub release

### Step 6: Publish Release

1. Go to **Releases** → find the draft release
2. Edit the title and review release notes
3. Click **Publish release**
4. This triggers **Build and Attach Release Assets** which builds and attaches NuGet packages

### Step 7: Publish to NuGet (Manual)

1. Go to **Actions** → **Publish to NuGet**
2. Click **Run workflow**
3. Optionally specify a release tag or use the latest
4. Toggle dry run off for production publish

## PR Validations

All PRs targeting `main` or `dev` run:

| Check | Description |
| ----- | ----------- |
| 🔨 CI Build & Test | .NET build and test |
| 📝 Header Check | License header validation |
| 🚫 Block Dev Release | Prevents `-dev` versions on `main` (main PRs only) |
| 🔄 Prepare Release | Version/badge/changelog updates (main PRs only) |

## Version Numbering

Follows [Semantic Versioning](https://semver.org/):

- **Major** (X.0.0): Breaking changes
- **Minor** (X.Y.0): New features, backward compatible
- **Patch** (X.Y.Z): Bug fixes, backward compatible
- **Prerelease** (X.Y.Z-alpha/beta/rc): Development versions

## Workflow Files

| File | Purpose |
| ---- | ------- |
| `ci.yml` | Build and test on push/PR |
| `headers-pr-check.yml` | License header validation on PR |
| `headers-milestone-pr.yml` | Normalize headers on milestone close |
| `pr-block-dev-to-main.yml` | Block `-dev` versions from main |
| `chore-version-badge.yml` | Auto-update README badges |
| `chore-version-main-release.yml` | Prepare release on dev→main PR |
| `chore-update-copyright-year.yml` | Auto-update copyright year annually (Jan 1st) |
| `milestone-release-draft.yml` | Create draft release on milestone close |
| `release-build.yml` | Build and attach assets on release publish |
| `publish-nuget.yml` | Manual NuGet publish |
| `sync-schema.yml` | Sync schema from ghjson-spec |

## Reusable Actions

| Action | Purpose |
| ------ | ------- |
| `actions/versioning/get-version` | Extract version from Directory.Build.props |
| `actions/versioning/update-version` | Update version in Directory.Build.props |
| `actions/documentation/update-badges` | Update README version/status badges |
| `actions/documentation/update-changelog` | Create release sections or add lines to changelog |

## Troubleshooting

**Problem:** Badge not updated after version change

- **Solution:** Run `chore-version-badge.yml` workflow manually

**Problem:** PR blocked by dev version check

- **Solution:** Remove `-dev` suffix from version in `Directory.Build.props`

**Problem:** License header check failing

- **Solution:** Run `tools/Update-LicenseHeaders.ps1` locally to fix headers
