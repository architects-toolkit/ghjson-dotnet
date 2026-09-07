# NuGet Publishing Guide

This document describes how to publish GhJSON.NET packages to NuGet.

## Automated Release Workflow

The project uses a tag-based release pipeline. See
[Branching and Release Workflow](./RELEASE_WORKFLOW.md) for the full model.

### Stage 1: Release Preparation → Tag → Release Draft

1. A maintainer runs **Release 1 - Prepare Release** (`release-1-prepare.yml`)
   with a bump, stage, and target branch (`main` or `release/X.Y`).
2. The workflow opens a `release-prep/<version>` PR containing only release
   metadata (`Directory.Build.props`, `README.md`, `CHANGELOG.md`).
3. When that PR merges, **Release 2 - Tag on Merge**
   (`release-2-tag-on-merge.yml`) creates the annotated bare tag
   (`<X.Y.Z>`, no `v` prefix), drafts a GitHub Release, and opens the
   post-release development-version bump PR on `main`.

**Workflow files**: `.github/workflows/release-1-prepare.yml`, `.github/workflows/release-2-tag-on-merge.yml`

### Stage 2: Release Published → Build & Attach Assets

When the release draft is published, a GitHub Action builds the packages:

- **Trigger**: Publishing a release
- **Actions**:
  1. Checkout code
  2. Update version in `Directory.Build.props`
  3. Build in Release configuration
  4. Run all tests
  5. Create NuGet packages
  6. Attach `.nupkg` and `.snupkg` files to the release

**Workflow file**: `.github/workflows/release-3-build.yml`

### Stage 3: Manual Trigger → Publish to NuGet (Trusted Publishing)

A manually triggered workflow publishes packages to NuGet using Trusted Publishing (short-lived API key via OIDC, no stored secrets):

- **Trigger**: Manual workflow dispatch
- **Inputs**:
  - `release_tag`: Specific release tag (optional, defaults to latest)
  - `dry_run`: Test mode without actually publishing
- **Actions**:
  1. Download `.nupkg` files from the release
  2. Request a temporary NuGet API key via `NuGet/login@v1` (OIDC)
  3. Push to NuGet using the short-lived key

**Workflow file**: `.github/workflows/publish-nuget.yml`

## Prerequisites

### 1. nuget.org Trusted Publishing policy

1. Sign in to [nuget.org](https://www.nuget.org/) with the account that owns the GhJSON packages.
2. Go to your username → **Trusted Publishing**.
3. Add a new policy with:
   - **Repository Owner**: `architects-toolkit`
   - **Repository**: `ghjson-dotnet`
   - **Workflow File**: `publish-nuget.yml`
   - **Environment**: *(leave empty unless you later bind the workflow to an environment)*
4. Save the policy. nuget.org will now trust the GitHub workflow to request short-lived publish tokens.

### 2. NuGet account username secret

Trusted Publishing still needs the nuget.org username (profile name) to request the temp key.

1. In GitHub → Settings → Secrets and variables → Actions → **New repository secret**
2. Name: `NUGET_USERNAME`
3. Value: your nuget.org username (not email)
4. Save

## Manual Publishing Steps

If you need to publish manually without GitHub Actions:

### 1. Build the packages

```bash
# Update version in Directory.Build.props
dotnet build --configuration Release
dotnet pack --configuration Release --output ./nupkgs
```

### 2. Push to NuGet (with Trusted Publishing)

```bash
# Get a short-lived API key (requires GITHUB_ID_TOKEN + nuget.org policy)
dotnet tool install -g NuGet.Credentials # if not already installed
nuget login -NonInteractive -Source https://api.nuget.org/v3/index.json -UserName YOUR_NUGET_USERNAME

# Push each package using the temp API key NuGet emitted to the standard config
dotnet nuget push ./nupkgs/GhJSON.Core.*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate

dotnet nuget push ./nupkgs/GhJSON.Grasshopper.*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

### 3. Verify on NuGet

Check the packages at:

- [GhJSON.Core](https://www.nuget.org/packages/GhJSON.Core)
- [GhJSON.Grasshopper](https://www.nuget.org/packages/GhJSON.Grasshopper)

**Note**: It may take a few minutes for packages to be indexed and appear in search.

## Package Information

| Package | Target Framework | Dependencies |
| --- | --- | --- |
| GhJSON.Core | netstandard2.0 | Newtonsoft.Json |
| GhJSON.Grasshopper | net7.0-windows, net7.0 | GhJSON.Core, Grasshopper, RhinoCommon |

## Troubleshooting

### "Package already exists"

The `--skip-duplicate` flag is used in the workflow to handle this. If publishing manually, add this flag or increment the version.

### "API key is invalid"

- Check the key hasn't expired
- Verify the glob pattern matches your package names
- Ensure you're using the correct NuGet source URL

### Build fails on GitHub Actions

- Check that the McNeel NuGet source is configured in `NuGet.Config`
- Verify all project references are correct
- Review the workflow logs for specific errors
