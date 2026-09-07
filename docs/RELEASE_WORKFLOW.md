# Branching and Release Workflow

This document describes the GhJSON.NET branch model and the CI/CD pipeline that
implements it. The model follows the SmartHopper release pipeline: a single
long-lived integration branch, temporary `release/X.Y` stabilization lines, and
immutable release tags.

## Branch model

| Branch pattern | Lifetime | Purpose |
| --- | --- | --- |
| `main` | Permanent | The only integration branch. Carries a dated development version (`X.Y.Z-dev.YYMMDD`) between releases. |
| `release/X.Y` | Temporary | Stabilization/maintenance line for the `X.Y` series. Created from a release tag, receives fixes, and is backported to `main` when the line is completed. |
| `release-prep/<version>` | Ephemeral | Automation branch holding only release metadata (`Directory.Build.props`, `README.md`, `CHANGELOG.md`). Merging it triggers tagging. |
| `hotfix/<X.Y.Z>-<slug>` | Ephemeral | Fix branch cut from a stable release tag. |
| `backport/*` | Ephemeral | Automation branches that carry fix commits to `main` or older release lines. |
| `chore/*`, `docs/*`, `feature/*`, `fix/*`, … | Ephemeral | Normal topic branches. All PRs target `main` (or a `release/X.Y` line for line-specific fixes). |

> `dev` no longer exists. Open pull requests and topic branches must target
> `main`, `release/X.Y`, or `hotfix/*` branches.

## Versioning

- The canonical version lives in `Directory.Build.props` → `<Version>`.
- `tools/Change-SolutionVersion.ps1` is the single writer: it updates the
  version, the README badges, and ensures `CHANGELOG.md` has an `[Unreleased]`
  section.
- Development versions on `main` are dated: `X.Y.Z-dev.YYMMDD`.
- Prerelease stages use sequenced suffixes: `X.Y.Z-alpha.N`, `-beta.N`, `-rc.N`.

## Tags and releases

- New release tags are **bare** versions: `1.1.2`, `1.2.0-beta.1`, `2.0.0`.
- Legacy `v`-prefixed tags (`v1.0.0`…`v1.1.1`) remain valid history: tag
  resolution accepts both forms, and a legacy `v` tag blocks reuse of the same
  bare version.
- Tags are created by `release-2-tag-on-merge.yml` at the `release-prep` merge
  commit and are the immutable release source of truth.
- GitHub Releases are created as **drafts**; a maintainer reviews and publishes
  them, which triggers the package build.

## Workflow map

### Checks

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `ci.yml` | push to `main`, PR to `main`/`release/**`/`hotfix/**`, `merge_group`, dispatch | Restore, Release build, tests + coverage |
| `headers-pr-check.yml` | PR / `merge_group` / dispatch | Apache-2.0 license header validation |
| `pr-version-validation.yml` | PR / `merge_group` / dispatch | Version progression + released-version reuse guard |

All three accept `workflow_dispatch` inputs (`pr_number`, `base_ref`,
`pr_title`) so `dispatch-required-pr-checks` can run them on
automation-created PR branches, which do not fire `pull_request` events for
required checks.

### Release pipeline

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `release-1-prepare.yml` | Manual | Computes the release version, updates version/badges/changelog, and opens a `release-prep/<version>` PR against `main` or `release/X.Y`. |
| `release-2-tag-on-merge.yml` | `release-prep/*` PR merged | Creates the annotated tag, drafts the GitHub Release, and opens + auto-merges the post-release dev-version bump PR on `main`. |
| `release-3-build.yml` | Release published | Builds and attaches `GhJSON.*.nupkg`/`.snupkg` assets to the release. |
| `publish-nuget.yml` | Manual | Downloads release assets and pushes to nuget.org via OIDC trusted publishing. |

### Stabilization lines

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `stabilization-1-start.yml` | Manual (`line`, `source` tag) | Creates `release/X.Y` from an existing release tag. |
| `stabilization-3-complete.yml` | Stable release published / manual | Cherry-picks the release line into a `backport/<version>` PR against `main` and closes the milestone. |

Stage promotion on a release line is manual: run `release-1-prepare.yml` with
`target-branch: release/X.Y` and the desired `stage` (`alpha` → `beta` → `rc` →
`stable`).

### Hotfixes

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `hotfix-1-start.yml` | Manual (`base-tag`, `slug`) | Creates `hotfix/<X.Y.Z>-<slug>` from a stable tag. |
| `hotfix-2-release.yml` | Manual | Cherry-picks the fix onto a `release-prep` PR, optionally backports to older active release lines, then deletes the hotfix branch. |

### Maintenance

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `version-bump.yml` | Manual | One-off version/stage bump PR on any base branch. |
| `chore-version-sync.yml` | Push to `main`/`release/**`/`hotfix/**` touching `**/*.cs`, `**/*.csproj`, or `Directory.Build.props` | Refreshes the dev-version date and README badges via a `chore/` or `docs/` PR. |
| `sync-schema.yml` | Daily / manual / schema-tool push | Syncs the embedded schema snapshot into a PR on `main`. |
| `chore-update-copyright-year.yml` | Yearly / manual | Copyright year update PR on `main`. |
| `headers-milestone-pr.yml` | Milestone closed | License-header normalization PR on `main`. |
| `pr-delete-auto-branches.yml` | PR closed | Deletes merged automation/topic branches. |
| `chore-cleanup-stale-branches.yml` | Weekly / manual | Deletes stale `chore/`, `cascade/`, `devin/` branches without open PRs. |

## Required repository configuration

The following must exist in the repository settings (not managed in code):

- **Rulesets**: `main` and `release/*` are protected (already configured).
- **Secrets**
  - `STABILITY_PAT_TOKEN` — classic PAT with `repo` + `workflow` scopes. Required
    because release tags and branches are created at commits that contain
    `.github/workflows/` files, which the `GITHUB_TOKEN` cannot manage.
  - `MISTRAL_API_KEY` — used by AI changelog review and release notes.
  - `NUGET_USERNAME` — nuget.org account for trusted publishing.
- **Variables (optional)**
  - `GHJSON_BOT_NAME` / `GHJSON_BOT_EMAIL` — identity for automation commits.
    Defaults to `github-actions` / `github-actions@users.noreply.github.com`.
- **Merge queue**: when enabled on `main`, the `merge_group` triggers on the
  check workflows validate queued merge groups.

## Typical flows

```text
Normal change:     topic ──PR──> main          (checks: CI, headers, version)
Release:           release-1 → release-prep PR ──merge──> main
                   release-2 → tag <version> + draft release + dev-bump PR
                   publish draft → release-3 attaches NuGet packages
                   publish-nuget.yml → nuget.org
Patch a release:   stabilization-1 (release/X.Y from tag)
                   fixes ──PR──> release/X.Y
                   release-1 on release/X.Y → … → publish
                   stabilization-3 backports the line to main
Hotfix:            hotfix-1 (hotfix/<tag>-<slug> from tag)
                   fix commits ──PR──> hotfix branch
                   hotfix-2 → release-prep PR + optional older-line backports
```
