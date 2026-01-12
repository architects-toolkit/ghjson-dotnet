# GhJSON .NET Library Architecture

> Comprehensive architecture proposal for the ghjson-dotnet library.

## Overview

The **ghjson-dotnet** library provides a robust, modular .NET implementation for working with GhJSON (Grasshopper JSON) documents. It is designed as an **independent library** that can be used standalone or integrated into larger systems like SmartHopper.

## Design Principles

1. **Modularity**: Each concern is isolated into dedicated modules with clear interfaces
2. **Extensibility**: New data types, component handlers, and validators can be added without modifying core code
3. **Separation of Concerns**: GhJSON.Core handles pure JSON operations; GhJSON.Grasshopper handles Rhino/GH integration
4. **Fail-Fast**: Explicit errors over silent fallbacks
5. **Testability**: All core logic testable without Grasshopper dependencies

---

## Project Structure

```text
ghjson-dotnet/
├── src/
│   ├── GhJSON.Core/                    # Pure .NET, no GH dependencies
│   │   ├── Models/                     # Document object model
│   │   ├── Serialization/              # JSON serialization
│   │   ├── Validation/                 # Schema validation
│   │   ├── Operations/                 # Document operations (NEW)
│   │   └── Migration/                  # Schema migration (NEW)
│   │
│   └── GhJSON.Grasshopper/             # Grasshopper integration
│       ├── Serialization/              # GH object serialization
│       ├── Canvas/                     # Canvas operations
│       ├── Graph/                      # Dependency analysis (NEW)
│       ├── Introspection/              # Runtime inspection
│       └── Validation/                 # GH-specific validation
│
├── tests/
│   ├── GhJSON.Core.Tests/
│   └── GhJSON.Grasshopper.Tests/
│
└── docs/
```

---

## GhJSON.Core

**Purpose**: Platform-independent operations on GhJSON documents. No Grasshopper or Rhino dependencies.

### Core API

```csharp
namespace GhJSON.Core
{
    /// <summary>
    /// Main entry point for GhJSON document operations.
    /// </summary>
    public static class GhJson
    {
        // Read/Write
        public static GhJsonDocument Read(string path);
        public static GhJsonDocument Read(Stream stream);
        public static GhJsonDocument Parse(string json);
        public static void Write(GhJsonDocument doc, string path, WriteOptions? options = null);
        public static string Serialize(GhJsonDocument doc, WriteOptions? options = null);
        
        // Validation
        public static ValidationResult Validate(GhJsonDocument doc, ValidationLevel level = ValidationLevel.Standard);
        public static ValidationResult Validate(string json, ValidationLevel level = ValidationLevel.Standard);
        
        // Fix/Repair
        public static FixResult Fix(GhJsonDocument doc, FixOptions? options = null);
        
        // Merge
        public static MergeResult Merge(GhJsonDocument target, GhJsonDocument source, MergeOptions? options = null);
        
        // Migration
        public static MigrationResult Migrate(GhJsonDocument doc, string targetVersion);
    }
}
```

### Models

```text
GhJSON.Core/Models/
├── Document/
│   ├── GhJsonDocument.cs           # Root document
│   ├── DocumentMetadata.cs         # Metadata (title, author, version, etc.)
│   └── GroupInfo.cs                # Group definitions
├── Components/
│   ├── ComponentProperties.cs      # Component definition
│   ├── ParameterSettings.cs        # Input/output parameter config
│   ├── ComponentState.cs           # UI state (value, locked, hidden)
│   ├── VBScriptCode.cs             # VB script sections
│   └── CompactPosition.cs          # Pivot position "X,Y"
└── Connections/
    ├── ConnectionPairing.cs        # From/To connection
    └── Connection.cs               # Connection endpoint (id, paramName/paramIndex)
```

### Serialization Module

**Responsibilities**: JSON serialization/deserialization with extensible data type handling.

```text
GhJSON.Core/Serialization/
├── DataTypes/
│   ├── IDataTypeSerializer.cs          # Serializer contract
│   ├── DataTypeRegistry.cs             # Serializer registry (extensible)
│   ├── DataTypeSerializer.cs           # Facade for serialize/deserialize
│   └── Serializers/
│       ├── ColorSerializer.cs          # "argb:A,R,G,B"
│       ├── TextSerializer.cs           # "text:value"
│       ├── NumberSerializer.cs         # "number:3.14"
│       ├── IntegerSerializer.cs        # "integer:42"
│       ├── BooleanSerializer.cs        # "boolean:true"
│       └── BoundsSerializer.cs         # "bounds:WxH"
├── GhJsonConverter.cs                  # Main JSON converter
├── CompactPosition.cs                  # Position serialization
└── EmptyStringIgnoreConverter.cs       # Utility converter
```

**Extensibility Pattern**:

```csharp
// Register custom serializer
DataTypeRegistry.Register(new MyCustomSerializer());

// Serializers implement IDataTypeSerializer
public interface IDataTypeSerializer
{
    string TypeName { get; }           // e.g., "Color", "Point3d"
    Type TargetType { get; }           // e.g., typeof(Color)
    string Prefix { get; }             // e.g., "argb", "pointXYZ"
    
    string Serialize(object value);
    object Deserialize(string value);
    bool Validate(string value);
}
```

### Validation Module

**Responsibilities**: Multi-level schema and structural validation.

```text
GhJSON.Core/Validation/
├── IValidator.cs                       # Validator contract
├── ValidationPipeline.cs               # Composable validator chain
├── ValidationResult.cs                 # Result with errors/warnings/info
├── ValidationLevel.cs                  # Enum: Minimal, Standard, Strict
└── Validators/
    ├── SchemaValidator.cs              # JSON Schema v1.0 validation
    ├── StructuralValidator.cs          # Required fields, valid GUIDs
    ├── ConnectionValidator.cs          # Connection integrity
    ├── IdConsistencyValidator.cs       # ID uniqueness and references
    └── MetadataValidator.cs            # Metadata completeness
```

**Validation Levels**:

| Level      | Checks                                                              |
|------------|---------------------------------------------------------------------|
| `Minimal` | Required fields only (name or componentGuid, id or instanceGuid) |
| `Standard` | + Connection validity, ID consistency, GUID formats |
| `Strict` | + Metadata completeness, property value ranges |

### Operations Module (NEW)

**Responsibilities**: Document manipulation operations.

```text
GhJSON.Core/Operations/
├── IDocumentOperation.cs               # Operation contract
├── FixOperations/
│   ├── IdAssigner.cs                   # Assign sequential IDs to components
│   ├── GuidGenerator.cs                # Generate instanceGuids from IDs
│   ├── MetadataPopulator.cs            # Add created/modified timestamps
│   ├── CountUpdater.cs                 # Update componentCount, connectionCount
│   └── DependencyResolver.cs           # List plugin dependencies (future DB)
├── MergeOperations/
│   ├── DocumentMerger.cs               # Merge two documents
│   ├── ConflictResolver.cs             # Handle ID/GUID conflicts
│   └── PositionAdjuster.cs             # Offset positions to avoid overlap
└── TidyOperations/
    ├── LayoutAnalyzer.cs               # Analyze component graph structure
    └── PivotOrganizer.cs               # Reorganize pivots based on flow
```

**Fix API**:

```csharp
public class FixOptions
{
    public bool AssignIds { get; set; } = true;
    public bool GenerateInstanceGuids { get; set; } = true;
    public bool PopulateMetadata { get; set; } = true;
    public bool UpdateCounts { get; set; } = true;
    public bool ResolveDependencies { get; set; } = false; // Requires DB
}

public class FixResult
{
    public GhJsonDocument Document { get; }
    public List<FixAction> Actions { get; }    // What was fixed
    public bool WasModified { get; }
}
```

### Migration Module (NEW)

**Responsibilities**: Schema version migration.

```text
GhJSON.Core/Migration/
├── IMigrator.cs                        # Migrator contract
├── MigrationPipeline.cs                # Ordered migration chain
├── MigrationResult.cs                  # Result with change log
└── Migrators/
    ├── V0_9_to_V1_0.cs                 # Example: pivot object → string
    └── V1_0_to_V1_1.cs                 # Future migrations
```

**Migration API**:

```csharp
public static class GhJson
{
    public static MigrationResult Migrate(GhJsonDocument doc, string targetVersion);
}

public class MigrationResult
{
    public GhJsonDocument Document { get; }
    public string FromVersion { get; }
    public string ToVersion { get; }
    public List<MigrationChange> Changes { get; }
}
```

---

## GhJSON.Grasshopper

**Purpose**: Grasshopper-specific serialization and canvas operations. Requires Grasshopper references.

### Core API

```csharp
namespace GhJSON.Grasshopper
{
    /// <summary>
    /// Main entry point for Grasshopper operations.
    /// </summary>
    public static class GhJsonGrasshopper
    {
        // Serialize (GH → GhJSON)
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options = null);
        
        // Deserialize (GhJSON → GH objects, not placed)
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options = null);
        
        // Get (read from canvas)
        public static GhJsonDocument Get(GetOptions? options = null);
        public static GhJsonDocument GetSelected();
        public static GhJsonDocument GetByGuids(IEnumerable<Guid> guids);
        [...]
        
        // Put (place on canvas)
        public static PutResult Put(
            GhJsonDocument document,
            PutOptions? options = null);
    }
}
```

### Grasshopper Serialization Module

**Responsibilities**: Convert between Grasshopper objects and GhJSON models.

```text
GhJSON.Grasshopper/Serialization/
├── GhJsonSerializer.cs                 # Main serializer (orchestrator only)
├── GhJsonDeserializer.cs               # Main deserializer (orchestrator only)
├── SerializationOptions.cs             # Serialization configuration
├── DeserializationOptions.cs           # Deserialization configuration
│
├── DataTypes/                          # Geometric type serializers
│   ├── GeometricSerializerRegistry.cs  # Auto-registers GH-specific serializers
│   ├── PointSerializer.cs              # "pointXYZ:x,y,z"
│   ├── VectorSerializer.cs             # "vectorXYZ:x,y,z"
│   ├── PlaneSerializer.cs              # "planeOXY:..."
│   ├── LineSerializer.cs               # "line2p:..."
│   ├── CircleSerializer.cs             # "circleCNRS:..."
│   ├── ArcSerializer.cs                # "arc3P:..."
│   ├── BoxSerializer.cs                # "boxOXY:..."
│   ├── RectangleSerializer.cs          # "rectangleCXY:..."
│   └── IntervalSerializer.cs           # "interval:min<max"
│
├── ComponentHandlers/                  # Component-specific handlers (NEW)
│   ├── IComponentHandler.cs            # Handler contract
│   ├── ComponentHandlerRegistry.cs     # Extensible registry
│   ├── DefaultComponentHandler.cs      # Generic component handling
│   ├── SliderHandler.cs                # GH_NumberSlider
│   ├── PanelHandler.cs                 # GH_Panel
│   ├── ValueListHandler.cs             # GH_ValueList
│   ├── ScriptHandler.cs                # Script components (C#/Python/VB)
│   ├── ToggleHandler.cs                # GH_BooleanToggle
│   ├── ColorSwatchHandler.cs           # GH_ColourSwatch
│   ├── ButtonHandler.cs                # GH_ButtonObject
│   └── ScribbleHandler.cs              # GH_Scribble
│
├── ParameterHandlers/                  # Parameter-specific handlers (NEW)
│   ├── IParameterHandler.cs            # Handler contract
│   ├── ParameterHandlerRegistry.cs     # Extensible registry
│   ├── DefaultParameterHandler.cs      # Generic parameter handling
│   └── ScriptParameterHandler.cs       # Script parameter specifics
│
├── SchemaProperties/                   # Property extraction/application
│   ├── PropertyManagerV2.cs            # Context-aware property management
│   ├── PropertyFilters/                # Property filtering by context
│   └── SerializationContext.cs         # Standard, Lite, Optimized
│
├── ScriptComponents/                   # Script component utilities
│   ├── ScriptComponentFactory.cs       # Script component detection
│   ├── ScriptComponentHelper.cs        # Language detection, code extraction
│   ├── ScriptParameterMapper.cs        # Script parameter handling
│   └── ScriptSignatureParser.cs        # Multi-language signature parsing
│
└── Shared/                             # Shared utilities
    ├── AccessModeMapper.cs             # GH_ParamAccess ↔ string
    ├── TypeHintMapper.cs               # Type hint formatting
    └── ParameterMapper.cs              # Generic parameter mapping
```

**Component Handler Pattern** (addressing hardcoded exceptions):

```csharp
/// <summary>
/// Handles serialization/deserialization for a specific component type.
/// </summary>
public interface IComponentHandler
{
    /// <summary>
    /// Component types this handler supports (by ComponentGuid or Type).
    /// </summary>
    IEnumerable<Guid> SupportedComponentGuids { get; }
    IEnumerable<Type> SupportedTypes { get; }
    
    /// <summary>
    /// Priority for handler selection (higher = preferred).
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Extract component state to GhJSON model.
    /// </summary>
    ComponentState? ExtractState(IGH_Component component);
    
    /// <summary>
    /// Extract universal value (slider value, panel text, etc.).
    /// </summary>
    object? ExtractValue(IGH_Component component);
    
    /// <summary>
    /// Apply component state from GhJSON model.
    /// </summary>
    void ApplyState(IGH_Component component, ComponentState state);
    
    /// <summary>
    /// Apply universal value.
    /// </summary>
    void ApplyValue(IGH_Component component, object value);
}

/// <summary>
/// Registry for component handlers. Extensible at runtime.
/// </summary>
public class ComponentHandlerRegistry
{
    public void Register(IComponentHandler handler);
    public void Unregister(Guid componentGuid);
    public IComponentHandler GetHandler(IGH_Component component);
    public IComponentHandler GetHandler(Guid componentGuid);
}
```

**Example Handler Implementation**:

```csharp
public class SliderHandler : IComponentHandler
{
    public IEnumerable<Guid> SupportedComponentGuids => 
        new[] { new Guid("57da07bd-ecab-415d-9d86-af36d7073abc") };
    
    public IEnumerable<Type> SupportedTypes => 
        new[] { typeof(GH_NumberSlider) };
    
    public int Priority => 100;
    
    public object? ExtractValue(IGH_Component component)
    {
        if (component is GH_NumberSlider slider)
        {
            var val = slider.Slider.Value;
            var min = slider.Slider.Minimum;
            var max = slider.Slider.Maximum;
            return $"{val}<{min}~{max}>";
        }
        return null;
    }
    
    public void ApplyValue(IGH_Component component, object value)
    {
        if (component is GH_NumberSlider slider && value is string str)
        {
            // Parse "value<min~max>" format
            var match = Regex.Match(str, @"^([\d.\-]+)<([\d.\-]+)~([\d.\-]+)>$");
            if (match.Success)
            {
                slider.Slider.Minimum = decimal.Parse(match.Groups[2].Value);
                slider.Slider.Maximum = decimal.Parse(match.Groups[3].Value);
                slider.SetSliderValue(decimal.Parse(match.Groups[1].Value));
            }
        }
    }
    
    // ... ExtractState, ApplyState implementations
}
```

### Canvas Module

**Responsibilities**: Place, connect, and group components on the Grasshopper canvas.

```text
GhJSON.Grasshopper/Canvas/
├── ComponentPlacer.cs                  # Place components at positions
├── ConnectionManager.cs                # Create/manage wires
├── GroupManager.cs                     # Create/manage groups
├── CanvasOperations.cs                 # High-level canvas API
└── UndoManager.cs                      # Undo/redo support
```

**Canvas API**:

```csharp
public class PutOptions
{
    public PointF? Offset { get; set; }                // Position offset
    public bool CreateConnections { get; set; } = true;
    public bool CreateGroups { get; set; } = true;
    public bool RegisterUndo { get; set; } = true;
    public ConflictBehavior GuidConflict { get; set; } = ConflictBehavior.GenerateNew;
}

public class PutResult
{
    public List<IGH_DocumentObject> PlacedObjects { get; }
    public Dictionary<int, IGH_DocumentObject> IdMapping { get; }
    public Dictionary<Guid, IGH_DocumentObject> GuidMapping { get; }
    public List<string> Errors { get; }
    public List<string> Warnings { get; }
}

public enum ConflictBehavior
{
    GenerateNew,    // Generate new GUIDs for conflicts
    Replace,        // Replace existing components
    Skip,           // Skip conflicting components
    Fail            // Fail the entire operation
}
```

### Graph Module (NEW)

**Responsibilities**: Dependency graph analysis for TidyUp operations.

```text
GhJSON.Grasshopper/Graph/
├── DependencyGraph.cs                  # Build graph from connections
├── TopologicalSort.cs                  # Order by dependencies
├── LayerAssigner.cs                    # Assign components to layers
├── LayoutEngine.cs                     # Calculate optimal positions
└── FlowAnalyzer.cs                     # Analyze data flow patterns
```

**TidyUp API** (part of GhJSON.Core.Operations but uses Graph):

```csharp
public class TidyOptions
{
    public float HorizontalSpacing { get; set; } = 150f;
    public float VerticalSpacing { get; set; } = 50f;
    public bool PreserveRelativePositions { get; set; } = false;
}
```

### Grasshopper Validation Module

**Responsibilities**: Grasshopper-specific validation (component existence, type compatibility).

```text
GhJSON.Grasshopper/Validation/
├── GrasshopperValidator.cs             # Main GH validator
├── ComponentExistenceValidator.cs      # Check if components exist in library
├── ConnectionTypeValidator.cs          # Check data type compatibility
└── ParameterValidator.cs               # Validate parameter configurations
```

---

## Data Type Serialization

### Format Standard

All data types use a **prefix:value** format for explicit type identification:

| Type      | Prefix        | Format                                              | Example                              |
|-----------|---------------|-----------------------------------------------------|--------------------------------------|
| Text | `text` | `text:value` | `text:Hello World` |
| Number | `number` | `number:value` | `number:3.14159` |
| Integer | `integer` | `integer:value` | `integer:42` |
| Boolean | `boolean` | `boolean:value` | `boolean:true` |
| Color | `argb` | `argb:A,R,G,B` | `argb:255,128,64,255` |
| Point3d | `pointXYZ` | `pointXYZ:x,y,z` | `pointXYZ:10.5,20.0,30.5` |
| Vector3d | `vectorXYZ` | `vectorXYZ:x,y,z` | `vectorXYZ:1.0,0.0,0.0` |
| Line | `line2p` | `line2p:x1,y1,z1;x2,y2,z2` | `line2p:0,0,0;10,10,10` |
| Plane | `planeOXY` | `planeOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz` | `planeOXY:0,0,0;1,0,0;0,1,0` |
| Circle | `circleCNRS` | `circleCNRS:cx,cy,cz;nx,ny,nz;r;sx,sy,sz` | `circleCNRS:0,0,0;0,0,1;5.0;5,0,0` |
| Arc | `arc3P` | `arc3P:x1,y1,z1;x2,y2,z2;x3,y3,z3` | `arc3P:0,0,0;5,5,0;10,0,0` |
| Box | `boxOXY` | `boxOXY:ox,oy,oz;xx,xy,xz;yx,yy,yz;x0,x1;y0,y1;z0,z1` | `boxOXY:0,0,0;1,0,0;0,1,0;-5,5;-5,5;0,10` |
| Rectangle | `rectangleCXY` | `rectangleCXY:cx,cy,cz;xx,xy,xz;yx,yy,yz;w,h` | `rectangleCXY:0,0,0;1,0,0;0,1,0;10,5` |
| Interval | `interval` | `interval:min<max` | `interval:0.0<10.0` |
| Bounds | `bounds` | `bounds:WxH` | `bounds:100x200` |

### Data Type Extensibility

```csharp
// Register a custom serializer
DataTypeRegistry.Register(new MyCustomTypeSerializer());

// Custom serializer example
public class MyCustomTypeSerializer : IDataTypeSerializer
{
    public string TypeName => "MyType";
    public string Prefix => "mytype";
    public Type TargetType => typeof(MyCustomType);
    
    public string Serialize(object value)
    {
        var v = (MyCustomType)value;
        return $"{Prefix}:{v.Property1},{v.Property2}";
    }
    
    public object Deserialize(string value)
    {
        var parts = value.Split(':')[1].Split(',');
        return new MyCustomType(parts[0], parts[1]);
    }
    
    public bool Validate(string value) => 
        value.StartsWith($"{Prefix}:") && value.Split(',').Length == 2;
}
```

---

## Implementation Phases

### Phase 1: Core Refactoring ✅

- [x] Implement `IComponentHandler` pattern
- [x] Create `ComponentHandlerRegistry`
- [x] Migrate existing hardcoded handlers to pattern
- [x] Add handler tests

### Phase 2: Operations Module ✅

- [x] Implement `FixOperations` (IdAssigner, GuidGenerator, etc.)
- [x] Implement `MergeOperations`
- [x] Add operation tests

### Phase 3: Migration & TidyUp ✅

- [x] Implement `MigrationPipeline`
- [x] Implement `TidyOperations` with graph analysis
- [x] Add migration/tidy tests

### Phase 4: API Consolidation ✅

- [x] Create `GhJson` static class facade
- [x] Create `GhJsonGrasshopper` static class facade
- [ ] Update documentation
- [ ] Breaking change migration guide

---

## Benefits

1. **No More Hardcoded Exceptions**: Component-specific logic isolated in handlers
2. **Easy Extension**: Add new component/parameter support without modifying core
3. **Clear Boundaries**: Core vs Grasshopper concerns separated
4. **Testable**: Core operations testable without GH dependencies
5. **Maintainable**: Each module has single responsibility
6. **Future-Proof**: Easy to add new schema versions, data types, operations

---

## Migration from Current Implementation

### Breaking Changes Expected

1. **Namespace Changes**: `GhJSON.Grasshopper.Serialization` → organized subnamespaces
2. **API Changes**: Direct method calls → static facade classes
3. **Handler Registration**: Auto-discovery vs explicit registration (configurable)

### Migration Path

1. Update to new API gradually
2. Old methods marked `[Obsolete]` initially
3. Full removal in next major version

---

## Related Documentation

- [GhJSON Schema v1.0](../schema/v1.0/ghjson.schema.json) - JSON Schema specification
- [NUGET-PUBLISHING.md](./NUGET-PUBLISHING.md) - Publishing guide
