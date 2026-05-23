# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### New Features

#### Automatic Component Layout

- **Dependency Graph Layout Engine**: New algorithm-based layout system with Sugiyama implementation
  - `LayoutEngine.CalculateLayout()` - Main entry point for layout calculations
  - `LayoutOptions` - Configurable spacing and algorithm selection
  - `LayoutResult` - Structured output with positions, islands, and diagnostics
  - `LayoutAlgorithm` enum - Extensible algorithm selector (currently Sugiyama only)
  - Internal modular Sugiyama implementation: LayerAssignment, EdgeConcentration, RowOrdering, CrossingMinimizer, CoordinateAssigner
  - `GraphBuilder` - Converts GhJsonDocument to internal graph representation
  - `IslandDetector` - Identifies disconnected component groups
- **Layout Façade Methods** in `GhJson.cs`:
  - `GhJson.CalculateLayout()` - Calculate optimal component positions using dependency graph analysis
  - `GhJson.AssignPivots()` - Apply calculated layout positions to document components
  - `GhJson.ReorganizePivots()` - Convenience method combining calculate and assign operations
- **Grasshopper-Aware Layout Refinements** in `GhJSON.Grasshopper/LayoutRefinements`:
  - `BoundsAwareSpacing` - Adjusts spacing based on actual component bounds (width/height)
  - `PortAlignment` - Aligns parameter components to input port positions
  - `CollisionResolver` - Prevents component overlaps and minimizes connection lengths
  - `LayoutRefinementEngine` - Orchestrates all refinement passes with configurable options
  - `LayoutRefinementOptions` - Configuration for enabling/disabling specific refinements

#### Fuzzy Name Resolution (AI-Friendly)

- **Smart component name matching** when exact names are unknown or misspelled
  - `ComponentNameResolver`: alias dictionary + fuzzy matching for Grasshopper component names
  - `ComponentTypeResolver`: pattern dictionary to prioritize/deprioritize certain component types (e.g. legacy script components)
  - `ParameterNameResolver`: alias dictionary + fuzzy matching for parameter names
  - `FuzzyMatcher`: core utility with exact, normalized, prefix, contains, and Levenshtein matching
  - `NameResolver`: unified public facade exposed via `GhJson.ResolveComponentName()` / `GhJson.ResolveParameterName()`
- **Automatic fallback** in deserialization and connection wiring when exact lookups fail
  - `ComponentInstantiator` falls back to fuzzy matching when exact name lookup fails during deserialization
  - `CanvasPlacer.GetParameter` falls back to fuzzy matching when exact parameter name lookup fails during connection wiring
  - Example: `"python"` now correctly resolves to `"Python 3 Script"` on Rhino 8 (previously failed)
- **Inspired by [Grasshopper MCP](https://github.com/alfredatnycu/grasshopper-mcp)** — an open-source MCP to connect Grasshopper with Claude Desktop (MIT License, compatible with our Apache License 2.0)

#### Canvas Operations

- **Delete operations** (`GhJSON.Grasshopper.DeleteOperations`) with full undo support
  - `GhJsonGrasshopper.Delete()`: Delete specific objects from the canvas by GUID with batch undo support
  - `GhJsonGrasshopper.Clear()`: Clear all objects from the canvas
  - `DeleteOptions`: Configuration for deletion behavior (redraw)
  - `DeleteResult`: Structured result with deleted/failed GUIDs and counts
  - All operations register proper Grasshopper undo events for Ctrl+Z support
- **Viewport filtering** — `CanvasSelector.WithViewport(RectangleF)` restricts queries to visible canvas area

#### Schema Validation

- **JSON Schema validation** against official GhJSON v1.0 specification
  - Validates raw JSON to catch unknown/invalid properties (catches errors that strong-typed deserialization would silently drop)
  - Three levels: `Minimal` (fast), `Standard` (with schema), `Strict` (with semantics)
  - Offline validation using embedded schema bundle (14 schemas including all extensions)

#### Document Building

- **Automatic ID assignment** in `DocumentBuilder.Build()`: Components lacking both `id` and `instanceGuid` now automatically receive sequential IDs before validation, eliminating the need for callers to manually assign IDs to new components

#### Diff and Patch Operations

- **Compare and apply document changes** (`GhJSON.Core.DiffOperations`, `GhJSON.Core.PatchModels`)
  - `GhJson.Diff(left, right, options?)` / `GhJson.DiffToPatch(...)` compare two `GhJsonDocument` instances and produce a `GhPatchDocument` describing the differences
  - `GhJson.ApplyPatch(baseDoc, patch, options?)` applies a `GhPatchDocument` to a base document, recording any conflicts in the result
  - `GhJson.PatchFromJson` / `GhJson.PatchToJson` / `GhJson.PatchFromFile` / `GhJson.PatchToFile` for `.ghpatch` serialization
  - `GhJson.ValidatePatch(...)` for structural patch validation
- **Identity precedence for matching**: `instanceGuid` > `id` > structural fingerprint (`componentGuid` + `name` + optional `pivot`)
- **Connection identity**: canonical `paramName`, with fallback to `paramIndex`
- **Diff options** (`DiffOptions`) defaults: ignore runtime messages, metadata counters and timestamps; pivots are diffed by default
- **Apply patch options** (`ApplyPatchOptions`) defaults: `VerifyBase = true` (refuses apply on base checksum mismatch), `ContinueOnConflict = true`, `RenumberCollidingAddedIds = true`
- **Conflict kinds**: `MatchNotFound`, `MatchAmbiguous`, `InstanceGuidCollision`, `ConnectionAlreadyPresent`, `ConnectionNotFound`, `DanglingMember`, `BaseChecksumMismatch`, `SchemaVersionMismatch`
- Implements the sibling `.ghpatch` profile defined in [ghjson-spec](https://architects-toolkit.github.io/ghjson-spec/)

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

- `ComponentNameResolver` Python aliases now resolve to `"Python 3 Script"` (Rhino 8 canonical name) instead of legacy `"Python Script"`
- `ComponentNameResolver` IronPython alias now resolves to `"IronPython 2 Script"` (Rhino 8 canonical name) instead of legacy `"IronPython Script"`
- `ComponentNameResolver` alias dictionary expanded with missing entries: `"python3"`, `"ghpython"`, `"python script"`, `"csharp script"`, `"c# component"`, `"number slider"` (with space), `"str"`, `"string"` (→ Text), `"streamfilter"`, `"filter"` (→ Stream Filter)
- `BaseScriptHandler.CanHandle` now also checks for extension key presence in `componentState.extensions`, preventing handler mismatch when component names are rewritten by alias resolution
- `ComponentNameResolver` alias verification — no longer returns aliases absent from the known set; falls back to fuzzy matching
- `GraphBuilder` GUID collision — id-only components now get stable synthetic keys instead of collapsing to `Guid.Empty`
- `CanvasDeleter` race condition — now blocks until UI thread completion; result reflects actual deletion outcome
- `PortAlignment` drift — removed accumulating `+ spacingY / 2` offset on chained connections
- `CrossingMinimizer` — added iteration cap (24) to prevent theoretical non-convergence





## [1.0.0] - 2026-02-08

- Initial release
