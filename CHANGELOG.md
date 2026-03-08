# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Automatic ID assignment in `DocumentBuilder.Build()`: Components lacking both `id` and `instanceGuid` now automatically receive sequential IDs before validation, eliminating the need for callers to manually assign IDs to new components
- CI/CD infrastructure adapted from SmartHopper project:
  - Reusable composite actions: `get-version`, `update-version`, `update-badges`, `update-changelog`
  - Automatic version badge update workflow (`chore-version-badge.yml`)
  - Dev-to-main PR validation: block `-dev` versions (`pr-block-dev-to-main.yml`)
  - Prepare release workflow on dev→main PR (`chore-version-main-release.yml`)
  - License header check on PRs to both `main` and `dev` branches
  - Automatic copyright year update workflow (`chore-update-copyright-year.yml`)
    - Scheduled to run annually on January 1st at 00:00 UTC
    - Updates all C# file license headers via `Update-LicenseHeaders.ps1`
    - Updates copyright year in `Directory.Build.props` via `Update-CopyrightYear.ps1`
    - Creates automated PR to dev with the changes
  - Release workflow documentation (`RELEASE_WORKFLOW.md`)
- Learning from Grasshopper MCP (https://github.com/alfredatnycu/grasshopper-mcp): This is an open-source MCP to connect Grasshopper with Claude Desktop using MIT License. This open-source license is compatible with our Apache License 2.0. That's why we are implementing some Grasshopper MCP features in GhJSON-dotnet. We implemented the following features:
  - Fuzzy name resolution for component and parameter names (`GhJSON.Core.NameResolution`)
    - `ComponentNameResolver`: alias dictionary + fuzzy matching for Grasshopper component names
    - `ComponentTypeResolver`: pattern dictionary to prioritize/deprioritize certain component types (e.g. legacy script components)
    - `ParameterNameResolver`: alias dictionary + fuzzy matching for parameter names
    - `FuzzyMatcher`: core utility with exact, normalized, prefix, contains, and Levenshtein matching
    - `NameResolver`: unified public facade exposed via `GhJson.ResolveComponentName()` / `GhJson.ResolveParameterName()`
- Fuzzy fallback in `ComponentInstantiator` when exact name lookup fails during deserialization
- Fuzzy fallback in `CanvasPlacer.GetParameter` when exact parameter name lookup fails during connection wiring
- Delete operations (`GhJSON.Grasshopper.DeleteOperations`):
  - `GhJsonGrasshopper.Delete()`: Delete specific objects from the canvas by GUID with batch undo support
  - `GhJsonGrasshopper.Clear()`: Clear all objects from the canvas
  - `DeleteOptions`: Configuration for deletion behavior (redraw)
  - `DeleteResult`: Structured result with deleted/failed GUIDs and counts
  - All operations register proper Grasshopper undo events for Ctrl+Z support
- `CanvasSelector.WithViewport(RectangleF)`: New fluent filter to restrict query results to objects within a given viewport rectangle. Applied as step 0 (before GUID/type/category/attribute filters) using bounds intersection.
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

### Changed

- **CanvasPlacer** now uses new layout API with Grasshopper-aware refinements for improved component positioning
- Updated README badges to `for-the-badge` style with version and status badges
- Downgrade Rhino and Grasshopper dependencies to 8.0
- TODO: Updated validation logic to use ghjson.schema.json v1.0

## [1.0.0] - 2026-02-08

- Initial release
