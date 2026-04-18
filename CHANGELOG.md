# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### New Features

#### Automatic Component Layout

- **Dependency Graph Layout Engine** with Sugiyama algorithm for automatic, intelligent component positioning
  - `GhJson.CalculateLayout()` — analyze connections and compute optimal positions
  - `GhJson.AssignPivots()` — apply calculated positions to components
  - `GhJson.ReorganizePivots()` — one-shot layout + apply
  - Handles disconnected component groups as separate "islands"
  - Detects and gracefully handles cyclic connections (reports via diagnostics)
- **Grasshopper-Aware Layout Refinements** for production-ready visuals
  - `BoundsAwareSpacing` — respects actual component sizes
  - `PortAlignment` — aligns source components to target input ports
  - `CollisionResolver` — prevents overlaps and shortens connection lengths
  - Configurable via `LayoutRefinementOptions`

#### Fuzzy Name Resolution (AI-Friendly)

- **Smart component name matching** when exact names are unknown or misspelled
  - Alias dictionary for common abbreviations (`"slider"` → `"Number Slider"`)
  - Fuzzy matching with Levenshtein distance for typos (`"Addtion"` → `"Addition"`)
  - Pattern-based type prioritization (prefers modern components over legacy)
  - Exposed via `GhJson.ResolveComponentName()` and `GhJson.ResolveParameterName()`
- **Automatic fallback** in deserialization and connection wiring when exact lookups fail
  - Example: `"python"` now correctly resolves to `"Python 3 Script"` on Rhino 8 (previously failed)

#### Canvas Operations

- **Delete operations** with full undo support
  - `GhJsonGrasshopper.Delete()` — remove specific objects by GUID
  - `GhJsonGrasshopper.Clear()` — remove all objects
  - Both operations block until complete and return detailed results
  - Single Ctrl+Z reverts entire batch
- **Viewport filtering** — `CanvasSelector.WithViewport(RectangleF)` restricts queries to visible canvas area

#### Schema Validation

- **JSON Schema validation** against official GhJSON v1.0 specification
  - Validates raw JSON to catch unknown/invalid properties (catches errors that strong-typed deserialization would silently drop)
  - Three levels: `Minimal` (fast), `Standard` (with schema), `Strict` (with semantics)
  - Offline validation using embedded schema bundle (14 schemas including all extensions)

### Noticeable Changes for End Users

- **Improved component positioning** — `CanvasPlacer` now uses the new layout engine with refinements for cleaner, more professional-looking Grasshopper definitions
- **More forgiving name matching** — AI-generated or hand-written GhJSON using informal names ("python", "slider", "pt") now works reliably
- **Better Rhino 8 compatibility** — component resolution correctly maps to current Grasshopper naming (e.g., "Python 3 Script" instead of legacy "Python Script")
- **Reliable undo** — deleting multiple objects via GhJSON now reverts with a single Ctrl+Z

### Noticeable Changes for Developers

- **Automatic ID assignment** — `DocumentBuilder.Build()` now auto-assigns sequential IDs to components lacking both `id` and `instanceGuid`, eliminating manual ID management
- **Stable layout keys** — components with only `id` (no `instanceGuid`) no longer collide into `Guid.Empty`; deterministic synthetic GUIDs ensure layout works for id-only documents
- **Headless-safe refinements** — `CollisionResolver` and `PortAlignment` gracefully degrade when no Grasshopper canvas is available (tests, automation)
- **Iterative layout algorithm** — `LayerAssignment` rewritten to avoid stack overflow on deep chains (thousands of nodes) and detect cycles instead of infinite recursion
- **Deterministic fuzzy matching** — `FuzzyMatcher` tie-breaking is now stable across runs

### Infrastructure & Tooling

- **Schema synchronization** — `tools/Sync-Schemas.ps1` downloads and validates schema drift from the official ghjson-spec repository; dynamically discovers all extension schemas
- **Automated workflows** — version badge updates, copyright year management, dev-to-main PR validation, license header checks
- **CI/CD** — composite actions for versioning, changelog updates, and release preparation
- **Dependencies** — Rhino and Grasshopper downgraded to 8.0 for broader compatibility

### Fixed

- `ComponentNameResolver` alias verification — no longer returns aliases absent from the known set; falls back to fuzzy matching
- `GraphBuilder` GUID collision — id-only components now get stable synthetic keys instead of collapsing to `Guid.Empty`
- `CanvasDeleter` race condition — now blocks until UI thread completion; result reflects actual deletion outcome
- `PortAlignment` drift — removed accumulating `+ spacingY / 2` offset on chained connections
- `CrossingMinimizer` — added iteration cap (24) to prevent theoretical non-convergence

## [1.0.0] - 2026-02-08

- Initial release
