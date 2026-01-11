# NuGet Publishing Guide

This document describes how to publish GhJSON.NET packages to NuGet.

## Automated Release Workflow

The project uses a 3-stage automated release workflow:

### Stage 1: Milestone Close → Release Draft

When a milestone is closed, a GitHub Action automatically creates a release draft:

- **Trigger**: Closing a milestone (e.g., "v1.0.0")
- **Action**: Creates a draft release with the milestone title as version
- **Output**: Draft release with placeholder release notes

**Workflow file**: `.github/workflows/milestone-release-draft.yml`

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

**Workflow file**: `.github/workflows/release-build.yml`

### Stage 3: Manual Trigger → Publish to NuGet

A manually triggered workflow publishes packages to NuGet:

- **Trigger**: Manual workflow dispatch
- **Inputs**:
  - `release_tag`: Specific release tag (optional, defaults to latest)
  - `dry_run`: Test mode without actually publishing
- **Actions**:
  1. Download `.nupkg` files from the release
  2. Push to NuGet using the API key

**Workflow file**: `.github/workflows/publish-nuget.yml`

## Prerequisites

### 1. NuGet API Key

Create an API key at [nuget.org](https://www.nuget.org/account/apikeys):

1. Sign in to nuget.org
2. Go to Account → API Keys
3. Create a new key:
   - **Name**: `ghjson-dotnet`
   - **Expiration**: 365 days
   - **Scopes**: Push
   - **Glob Pattern**: `GhJSON.*`
4. Copy the key immediately (it won't be shown again)

### 2. GitHub Secret

Add the API key as a repository secret:

1. Go to repository Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Name: `NUGET_API_KEY`
4. Value: Your NuGet API key
5. Click "Add secret"

## Manual Publishing Steps

If you need to publish manually without GitHub Actions:

### 1. Build the packages

```bash
# Update version in Directory.Build.props
dotnet build --configuration Release
dotnet pack --configuration Release --output ./nupkgs
```

### 2. Push to NuGet

```bash
# Push each package
dotnet nuget push ./nupkgs/GhJSON.Core.*.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./nupkgs/GhJSON.Grasshopper.*.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### 3. Verify on NuGet

Check the packages at:
- https://www.nuget.org/packages/GhJSON.Core
- https://www.nuget.org/packages/GhJSON.Grasshopper

**Note**: It may take a few minutes for packages to be indexed and appear in search.

## Package Information

| Package | Target Framework | Dependencies |
|---------|------------------|--------------|
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
