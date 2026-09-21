# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### New Features

#### Port-Aware Layout Engine

- **Real component bounds feed the core layout**
  - `LayoutOptions.NodeSizeProvider` (`Func<Guid, SizeF?>`) lets callers supply measured bounds; the engine falls back to the default 100x60 when the provider is unset, returns null, or throws
  - `GhJsonGrasshopper.CreateNodeSizeProvider(GH_Document? document = null)` exposes a live-bounds provider reading `Attributes.Bounds`; `gh_put` uses it automatically so layouts reflect real component geometry
  - Column widths and row heights are per-column/per-row maxima instead of global units, so a single large component only inflates its own band
- **Port-aware ordering and placement**
  - Layout nodes track estimated input/output port counts and each edge's connected port indices (`To.ParamIndex` = input, `From.ParamIndex` = output)
  - Crossing counting, barycenter ordering, and weighted-median sweeps operate on port-level endpoints, so fan-out wires occupy distinct rows and a lower output port deterministically orders its target below a higher port's
  - Coordinate assignment pulls each node toward the median of its parents' connected output port rows (`pivotY = parentPortY - inputPortOffset`), falling back to row bands when no parent is resolvable
  - Long edges keep their real port indices through dummy routing chains; dummy segments use index -1
- **Port-to-port wire alignment** (`gh_put` refinement)
  - `PortAlignment.AlignToPorts` aligns each source so its wire enters the target's specific input port horizontally, using the target input port's and source output port's real bounds-center deltas
  - Floating parameters (panels, sliders, value params) participate as both targets (left-edge input grip) and sources (right-edge output grip)
  - The alignment/collision pair now iterates up to three convergence passes (0.5 px epsilon) with collision resolution always getting the final word in each pass

### Changed

- **Compact default spacing**: `SpacingX = 80`, `SpacingY = 28`, `IslandSpacingY = 100` (edge-to-edge gaps, previously looser center-based spacing)
- **Tolerance-based position clustering**: `BoundsAwareSpacing` and `CollisionResolver` group components into columns/rows when positions differ by <= 1 px instead of integer truncation/rounding
- **Obsolete proxies never resolve by name**: exact-name and fuzzy resolution in `ComponentInstantiator` exclude proxies flagged `IGH_ObjectProxy.Obsolete`. An obsolete component is only ever instantiated through an explicit `ComponentGuid`, so round-tripping old files still works
- **Islands stack vertically with a shared left edge** instead of horizontal shelf-packing, ordered by their original canvas position (top-to-bottom, left-to-right) when the document carries pivots, or largest-first otherwise. `LayoutOptions.IslandWrapWidth` was removed
- **Island normalization uses bounding-box edges**: each island's origin is its bounds top-left rather than its leftmost center, so island edges actually align when stacked

### Fixed

- **Renamed "Deconstruct Point" resolves to the Rhino 8 "Deconstruct" component**: the legacy `Deconstruct Point` (`670fcdba-…`) is obsolete, so name resolution refused it and fuzzy matching fell back to `Construct Point`. New aliases (`deconstructpoint`, `pointdeconstruct`, `pointcoordinates`, `pdecon`) map deterministically to `Deconstruct`, bypassing fuzzy matching entirely
- **Layout positions are converted to per-object pivots before placement**: the layout engine works in bounds-center coordinates, but Grasshopper pivots differ per object type (components pivot at center, sliders/panels/floating parameters at top-left). New `PivotSemantics.CenterToPivot`/`BoundsCenter` helpers perform the conversion in `CanvasPlacer`, fixing panels and sliders landing offset relative to components
- **`CanvasBoundsCalculator` uses rendered bounds directly** instead of deriving edges from `Pivot + size`, which mis-measured center-pivot objects
- **Panels size to their content** when the GhJSON carries no explicit `bounds`: `PanelHandler` estimates width/height from text, font, multiline and wrap instead of leaving the oversized default

## [1.1.2] - 2026-09-07

### Changed

- **CI/CD**: Migrated to a single-main release pipeline with stabilization branches

### Removed

- Dev-era CI/CD workflows superseded by the new pipeline

## [1.1.1] - 2026-07-05

### New Features

#### Paginated GhJSON Documents

- **Boundary connections** preserved across pages instead of silently dropped
  - `GhJsonConnection.Boundary` flag marks connections that reference components outside the current page
  - `GhJson.SegmentDocument()` now keeps any connection that touches the requested page and marks cross-page ones as boundary
  - `GhJsonValidator` allows boundary connections and reports them as informational messages instead of errors
- **Pagination metadata** added to `GhJsonMetadata` via `GhJsonPagination`
  - `page`, `pageSize`, `totalPages` fields
  - Automatically populated by `GhJson.SegmentDocument()` when the document spans multiple pages
  - Pagination is always emitted for multi-page documents, even when `IncludeMetadata` is disabled
  - Pagination is omitted for single-page documents, even when `IncludeMetadata` is enabled
- **Page joining** via `GhJson.JoinPages()` reuses `DocumentMerger` to reassemble paginated documents
  - Deduplicates overlapping components by ID and instance GUID
  - Resolves boundary connections when both endpoints are present
  - Merges group members across pages and removes empty groups
  - Strips pagination metadata and recomputes counts

#### Component State Serialization

- **File Path floating parameter support** (`GhJSON.Grasshopper.Serialization.ObjectHandlers.FilePathHandler`)
  - Serializes the file filter and `ExpireOnFileEvent` flag via the new `gh.filepath` extension schema
  - The actual file path is preserved through existing internalized data serialization
  - Added `gh.filepath` schema to the official GhJSON v1.0 extension registry

#### Thread-Safe Canvas Operations

- **Thread-safe connection helper** (`GhJSON.Grasshopper.ConnectionOperations.CanvasConnector`)
  - `GhJsonGrasshopper.Connect()` now delegates to `CanvasConnector`, marshaling canvas access to the Rhino UI thread, recording a single undo event, and blocking until completion, matching the deletion helper pattern
  - Added `GhJsonGrasshopper.Disconnect()` to remove wires between components with the same UI-thread safety and undo support
  - Added `GhJsonGrasshopper.CaptureExternalConnections()` to capture all wires that connect a set of components to components outside the set, enabling replacement workflows to preserve external wiring
  - Prevents "Cross-thread operation not valid" errors when connecting or disconnecting components from non-UI threads (e.g., MCP/AI tool calls)

#### Topology Classification Facade

- **`GhJsonGrasshopper.ClassifyTopology()`** facade method exposes the topological classification (start/end/middle/isolated nodes) of a set of canvas objects, delegating to the internal `ConnectionWalker.Classify`
  - Made `TopologyClassification` public as the DTO returned by the facade
  - `ConnectionWalker` remains `internal`

### Changed

- **Number Slider value format documented**
  - `componentState.extensions["gh.numberslider"].value` uses the compact format `current<min~max>` (e.g. `10<5~50>` for min=5, value=10, max=50)
  - Trailing zeros are normalized on round-trip, so `10<5~50.00>` is reported back as `10<5~50>`
  - Documented in the GhJSON.NET Usage Guide, `NumberSliderHandler` XML docstring, and the `gh_get`/`gh_put` MCP tool descriptions

- **Integer-only pivot coordinates**
  - `GhJsonPivot.X` and `GhJsonPivot.Y` are now `int` and the v1.0 schema only accepts integer coordinates
  - Compact `"X,Y"` and object `{x,y}` formats no longer allow decimal values
  - Fractional Grasshopper canvas coordinates are rounded to the nearest integer when serialized
  - `PivotConverter` is now `public` so it can be instantiated from any consuming assembly
- **Paginated output respects `IncludeMetadata`**
  - When `IncludeMetadata` is `false`, the metadata block is suppressed unless pagination is required (multi-page documents)
  - When pagination is required, only the `pagination` object is emitted; title, counts, generator, and version fields are omitted
  - Single-page documents never include `pagination`, even when `IncludeMetadata` is `true`

- **GhPatch add operations no longer accept `instanceGuid`**
  - `patch.components.add` and `patch.groups.add` entries must not specify `instanceGuid`; the updated GhPatch schema prohibits it and `PatchValidator` reports a clear error with the JSON path.
  - `PatchApplier` no longer checks for `instanceGuid` collisions on add; the `PatchConflictKind.InstanceGuidCollision` kind has been replaced by `PatchConflictKind.IdCollision` for id collisions when `RenumberCollidingAddedIds` is disabled.
  - `SchemaLoader` now loads the main GhJSON schema when loading the patch schema so that the patch schema can reference `ghjson.schema.json` definitions.
- **AI-generated release descriptions**
  - `milestone-release-draft.yml` now uses the Mistral AI Chat API to generate a developer-oriented release description of new features, breaking changes, and deprecations from the relevant changelog section.
- Updated GitHub Actions workflow and reusable action references to Node 24-compatible versions.

### Fixed

- `GhJsonGrasshopper.Put()` now records placed components and groups as one Grasshopper add-object undo event, so users can remove the placed network with Ctrl+Z.
- **Confusing GhJSON validator error messages** when schema validation uses `anyOf`/`oneOf` identity branches
  - `GhJsonValidator.FlattenDetails` and `PatchValidator.FlattenDetails` now suppress errors from failing `anyOf`/`oneOf` branches when another branch is valid
  - Previously, valid components could report misleading "missing instanceGuid/componentGuid" errors alongside the real issue (e.g., an unknown property)
- `ComponentNameResolver` `"string"` and `"str"` aliases now resolve to `"Panel"` (Grasshopper Panel) instead of ambiguous `"Text"`, which could resolve to third-party components such as Mandrill Text
- **Runtime data schema sync**
  - The v1.0 GhJSON schema now allows `runtimeData` on `inputSettings`/`outputSettings` entries.
  - This matches the `RuntimeData` property on `GhJsonParameterSettings` and the `IncludeRuntimeData` option; documents and patches that contain volatile data validate again.
  - `runtimeData` is also documented as a volatile field to drop during GhPatch checksum normalization.

## [1.1.0] - 2026-06-19

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

- `PatchValidator` now loads the patch schema as a bundle with the main GhJSON schema, registers all schemas in the per-evaluation `SchemaRegistry`, and uses the shared `SchemaEvaluationLock`; this resolves `ghjson.schema.json#/$defs/...` `$ref`s when `ValidatePatch` is called with `preferOnline: true`.
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
