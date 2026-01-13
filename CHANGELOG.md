# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Property Handling Architecture Documentation**: Added comprehensive documentation at `docs/Architecture/PropertyHandling.md` explaining the three-tier property system (ComponentHandlerRegistry, DataTypeRegistry, PropertyManagerV2) and their separation of concerns.

- **Official JSON Schema validation**: `GhJSON.Core.Validation.GhJsonValidator` now validates GhJSON documents against the official v1.0 JSON Schema (`https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json`) with an embedded offline fallback.

- **Test Coverage Analysis**: Analyzed existing test suite for schema compliance and feature coverage
  - Existing tests cover: Models, Serialization (basic types, colors, compact position), Validation, Operations, Migration
  - GhJSON.Core.Tests: 21 test files covering core functionality
  - GhJSON.Grasshopper.Tests: 7 component-based test files for Grasshopper integration

- **Documentation**: Added a documentation index (`docs/index.md`) and comprehensive usage guides under `docs/Usage/`.

- **Grasshopper Get/Put extensions**:
  - Added `GhJSON.Grasshopper.Canvas.GetOptions` and `GhJsonGrasshopper.GetWithOptions()` to support connection-depth expansion and connection trimming.
  - Expanded `GetScope` with common canvas scopes (errors/warnings/remarks, enabled/disabled, preview on/off, start/end/middle/isolated nodes, params/components).
  - Added optional filter flags to `GetOptions`: `ObjectKinds`, `NodeRoles`, `Attributes`.
  - Added `GhJsonGrasshopper.ConnectComponents()` façade for creating wires between components.
  - Extended `PutOptions` with `PreserveExternalConnections` and `CapturedConnections` to support edit-mode external wiring preservation.

#### Component Handler Pattern (Phase 1 Core Refactoring)

- **IComponentHandler interface**: Extensible contract for component-specific serialization/deserialization logic
  - `ExtractState()` / `ApplyState()` for component state management
  - `ExtractValue()` / `ApplyValue()` for universal value handling
  - Support for GUID-based and Type-based handler matching
  - Priority system for handler selection (higher = preferred)
- **ComponentHandlerRegistry**: Centralized registry for component handlers
  - Auto-registration of built-in handlers
  - Runtime extensibility via `Register()` / `Unregister()`
  - GUID and Type caching for fast handler lookup
  - Thread-safe handler management
- **Built-in component handlers**:
  - `SliderHandler` - GH_NumberSlider (value format: `current<min~max>`, rounding mode)
  - `PanelHandler` - GH_Panel (text, color, bounds, multiline, wrap, draw settings)
  - `ValueListHandler` - GH_ValueList (list mode, selected items/indices)
  - `ToggleHandler` - GH_BooleanToggle (boolean value)
  - `ColorSwatchHandler` - GH_ColourSwatch (color in argb/rgba format)
  - `ButtonHandler` - GH_ButtonObject (normal/pressed expressions)
  - `ScribbleHandler` - GH_Scribble (text, font, corner positions)
  - `ScriptHandler` - Script components (C#/Python/VB code, standard output visibility)
  - `DefaultComponentHandler` - Fallback for generic locked/hidden state
- **Serializer/Deserializer integration**:

### Changed

- **Legacy Property System Removal**: Complete migration from legacy PropertyHandlerRegistry to ComponentHandlers
  - Removed `PropertyHandlerRegistry.cs`, `SpecializedPropertyHandlers.cs`, and `IPropertyHandler.cs`
  - Created `GenericPropertyManager.cs` - simplified static utility for generic properties
  - PropertyManagerV2 now uses GenericPropertyManager instead of PropertyHandlerRegistry
  - PropertyFilterConfig simplified - all component categories removed (now handled by ComponentHandlers)
  - ValueListHandler now handles ListItems serialization/deserialization (migrated from ValueListItemsPropertyHandler)
  - Added `ValueListItem` model class in GhJSON.Core for structured list item serialization
  - ComponentState extended with `ListItems` property for ValueList components

- **Property Duplication Cleanup**: Removed component-specific properties from PropertyFilterConfig that are now handled by ComponentHandlers
  - Slider properties (Minimum, Maximum, Range, Decimals, Limit, DisplayFormat) removed from PropertyFilter - handled by SliderHandler via ComponentState
  - ValueList properties (ListMode, ListItems, SelectedIndices) removed from PropertyFilter - handled by ValueListHandler via ComponentState
  - Script properties (Script, MarshInputs, MarshOutputs, VariableName) removed from PropertyFilter - handled by ScriptHandler via ComponentState
  - This eliminates duplication between the Properties dictionary and ComponentState object
  - `GhJsonSerializer.Serialize()` accepts optional `ComponentHandlerRegistry`
  - `GhJsonDeserializer.Deserialize()` accepts optional `ComponentHandlerRegistry`
  - Default registry used when not specified

#### Operations Module (Phase 2)

- **FixOperations**: Orchestrated document repair operations
  - `IdAssigner` - Assigns sequential IDs to components missing valid IDs
  - `GuidGenerator` - Generates instance GUIDs for components without them
  - `MetadataPopulator` - Populates metadata fields (timestamps, schema version)
  - `CountUpdater` - Updates component/connection counts in metadata
  - `DocumentFixer` - Orchestrates all fix operations with configurable `FixOptions`
- **MergeOperations**: Document merging with conflict resolution
  - `DocumentMerger` - Merges source document into target with ID remapping
  - `ConflictResolver` - Handles GUID conflicts (TargetWins, SourceWins, KeepBoth, Fail)
  - `PositionAdjuster` - Offsets positions to avoid overlap during merge
  - `MergeOptions` - Configures merge behavior (conflict resolution, position adjustment)
- **Operation contracts**:
  - `IDocumentOperation` interface for extensible operations
  - `FixOptions` / `FixResult` for fix operation configuration and results
  - `MergeResult` for merge operation statistics

#### Migration Module (Phase 3)

- **MigrationPipeline**: Orchestrated schema version migrations
  - `IMigrator` interface for versioned migrators
  - `MigrationPipeline.Default` - Pipeline with all built-in migrators
  - `MigrationResult` with change tracking and error handling
  - `NeedsMigration()` check for version detection
- **Built-in migrators**:
  - `V0_9_to_V1_0_PivotMigrator` - Converts pivot from object `{X,Y}` to string `"X,Y"`
  - `V0_9_to_V1_0_PropertyMigrator` - Renames legacy property names to v1.0 schema

#### TidyUp Operations (Phase 3)

- **LayoutAnalyzer**: Graph structure analysis for layout optimization
  - Identifies source/sink nodes (no inputs/outputs)
  - Calculates node depths (longest path from source)
  - Groups nodes by depth level
  - Identifies disconnected component islands
- **PivotOrganizer**: Reorganizes pivots based on graph flow
  - Horizontal arrangement by dependency depth
  - Vertical stacking within depth levels
  - Separate island handling with spacing
  - Configurable spacing via `PivotOrganizerOptions`
- **DocumentTidier**: Orchestrates tidy operations
  - `TidyAll()` - Apply all tidy operations
  - `AnalyzeLayout()` - Analyze without modifying
  - Configurable via `TidyOptions`

#### API Consolidation (Phase 4)

- **GhJson static facade** (`GhJSON.Core.GhJson`): Main entry point for platform-independent GhJSON operations
  - `Read()` / `Parse()` - Load documents from file, stream, or JSON string
  - `Write()` / `Serialize()` - Save documents with configurable `WriteOptions`
  - `Validate()` / `IsValid()` - Multi-level document validation
  - `Fix()` / `FixMinimal()` - Document repair operations
  - `Migrate()` / `NeedsMigration()` - Schema version migration
- **GhJsonGrasshopper static facade** (`GhJSON.Grasshopper.GhJsonGrasshopper`): Main entry point for Grasshopper operations
  - `Serialize()` - Convert GH objects to GhJSON document
  - `Deserialize()` - Create GH objects from GhJSON (without placement)
  - `Get()` / `GetSelected()` / `GetByGuids()` - Read from canvas
  - `Put()` - Place document on canvas with `PutOptions` (connections, groups, layout)
  - `Validate()` / `IsValid()` - Grasshopper-specific validation
  - `RegisterHandler()` - Register custom component handlers
- **PutOptions configuration class**: Configures canvas placement behavior
  - `Offset`, `Spacing`, `UseExactPositions`, `UseDependencyLayout`
  - `CreateConnections`, `CreateGroups`, `PreserveInstanceGuids`
  - `ApplyComponentState`, `ApplySchemaProperties`, `ApplyParameterSettings`
- **PutResult class**: Result of Put operations with placed objects and mappings
- **WriteOptions class**: Configures JSON serialization (indentation, null handling)
- **Automatic registry initialization**: `GhJsonGrasshopper.Initialize()` ensures all serializers and handlers are registered

#### Core Features

- **Instance GUID injection**: Added `GhJsonFixer.InjectMissingInstanceGuids()` to generate GUIDs for components that don't have them (useful for AI-generated documents)
- **Optional GUID injection in FixAll**: Added `injectMissingGuids` parameter to `GhJsonFixer.FixAll()` to support documents without instanceGuids

#### Script Component Support

- **Full script component deserialization parity**: Comprehensive reflection-based script component handling
  - Script code application with optional C# type hint injection
  - Parameter rebuild from `InputSettings`/`OutputSettings` using reflection
  - VB.NET 3-section code support (Imports/Script/Additional)
  - Standard output parameter visibility control (`ShowStandardOutput`)
- **Script parameter creation**: Added `ScriptParameterMapper.CreateParameter()` for reflection-based script parameter instantiation
  - Creates `ScriptVariableParam` for C#/Python/IronPython
  - Creates `Param_ScriptVariable` for VB.NET
  - Applies `Optional` and `AllowTreeAccess` properties
  - Supports type hint application via `TypeHints.Select()`
- **Script parameter extraction**: Added `ScriptParameterMapper.ExtractSettings()` to capture script-specific parameter data
  - Extracts variable names, type hints, Optional flags
  - Preserves principal parameter marking
- **Script state serialization**: Enhanced serializer to capture script-specific state
  - VB.NET code serialized as `ComponentState.VBCode` (3 sections)
  - Standard output visibility serialized as `ComponentState.ShowStandardOutput`
  - Script-aware parameter extraction using `ScriptParameterMapper`

#### Serialization & Deserialization

- **Staged deserialization flow**: `GhJsonDeserializer` now creates components without positioning or connection creation
  - Exposes `IdMapping` and `GuidMapping` in `DeserializationResult` for staged operations
  - Positioning and connection creation delegated to canvas utilities
- **GUID-first component lookup**: Components resolved by GUID first, then by name (avoids ambiguous name matches)
- **NickName-based connection matching**: Connections use parameter `NickName` (fallback to `Name`) for robust wiring
  - Serializer emits `NickName` in connection endpoints
  - `ConnectionManager` and `DependencyGraphUtils` resolve by ParamIndex → NickName → Name
- **Group serialization**: Serializer extracts group information with member ID mapping

### Changed

#### Breaking Changes

- **Renamed `GrasshopperDocument` to `GhJsonDocument`**: Clearer naming for the root document model
- **Deserialization flow**: `GhJsonDeserializer.Deserialize()` no longer creates connections or sets component pivots
  - Use `ComponentPlacer` for positioning
  - Use `ConnectionManager.CreateConnections()` for wiring
  - Use `GroupManager.CreateGroups()` for group recreation
- **Connection endpoint matching**: Changed from `Name`-only to `ParamIndex` → `NickName` → `Name` priority
- **Persistent data handling**: Removed `ParameterSettings.PersistentData` extraction (handled via `SchemaProperties`)

#### Improvements

- **Script component state**: Avoid re-applying script code in `ApplyComponentState()` to prevent parameter regeneration
- **C# type hint injection**: Minimal injection replaces `object` parameter types with base types from `TypeHint`
- **Geometric serializer initialization**: Ensured in both serializer and deserializer for consistent behavior

### Fixed

- **Script parameter type hints**: Type hints now correctly applied via reflection using `TypeHints.Select()`
- **VB script parameter handling**: Proper `Param_ScriptVariable` instantiation for VB.NET components
- **Parameter access modes**: Correct mapping between `GH_ParamAccess` and string representations

### Removed

- **Legacy connection creation**: Removed in-deserializer connection helper (replaced by `Canvas.ConnectionManager`)
- **Direct pivot setting**: Removed from deserializer (delegated to placement utilities)
- **Dead code cleanup**: Removed unused type-specific methods from serializer/deserializer
  - `GhJsonSerializer`: Removed `SerializeComponent`, `SerializeParameter`, `ExtractComponentState`, `ExtractUniversalValue`, `ExtractVBScriptCode`, `ExtractScriptCode`, `FormatSliderValue` (all duplicated in component handlers)
  - `GhJsonDeserializer`: Removed `ApplyComponentState`, `ApplyUniversalValue`, `ApplySliderValue`, `ParseColor`, `ApplyPanelAppearance` (handlers are used via `handler.ApplyState()` instead)

## [1.0.0] - Initial Release

### Initial Features

- Core GhJSON serialization and deserialization
- Component and parameter property handling
- Connection and group support
- Schema property system
- Data type serializers for geometric types
- Validation and fixing utilities
- Dependency graph layout utilities
