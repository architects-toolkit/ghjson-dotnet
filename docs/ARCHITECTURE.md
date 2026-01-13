# GhJSON .NET Library Architecture

> Comprehensive architecture proposal for the ghjson-dotnet library.

## Overview

The **ghjson-dotnet** library provides a robust, modular .NET implementation for working with GhJSON (Grasshopper JSON) documents. It is designed as an **independent library** that can be used standalone or integrated into larger systems like SmartHopper.

## Design Principles

1. **Modularity**: Each concern is isolated into dedicated modules with clear interfaces
2. **Extensibility**: New data types, component handlers, validators, etc. can be added without modifying core code
3. **Separation of Concerns**: GhJSON.Core handles pure JSON operations; GhJSON.Grasshopper handles Rhino/GH integration
4. **Fail-Fast**: Explicit errors over silent fallbacks
5. **Testability**: All core logic testable without Grasshopper dependencies
6. **Compliant**: Is fully compliant with ghjsonschema-v1.0
7. **Organized**: Is comprehensively organized following ghjson schema structure

---

## Project Structure

### Concept

- The project must be organized following the ghjson schema structure.
- The façade classes must be the only entry points to the library. All other classes must be internal.
- The façade classes must be organized in a comprehensive manner, grouping related functionality based on how user will need them.
- In a second level, the project must be organized following the main methods exposed in the façade classes.

### File structure

```text
ghjson-dotnet/
├── src/
│   ├── GhJSON.Core/                    # Pure .NET, no GH dependencies
│   │   ├── SchemaModels/               # Document and subobjects models
│   │   ├── SchemaMigration/            # Schema migration
│   │   ├── Serialization/              # JSON serialization
│   │   ├── Deserialization/            # JSON deserialization
│   │   ├── Validation/                 # Schema validation
│   │   ├── FixOperations/              # Schema fix operations
│   │   ├── MergeOperations/            # Schema merge operations
│   │   ├── TidyUpOperations/           # Schema tidy up operations
│   │   └── GhJson.cs                   # Main façade entry point
│   │
│   └── GhJSON.Grasshopper/             # Grasshopper integration
│       ├── Serialization/              # GH object serialization
│       ├── Deserialization/            # GH object deserialization
│       ├── GetOperations/              # GH object retrieval operations from canvas
│       ├── PutOperations/              # GH object placement operations on canvas
│       ├── Validation/                 # GH-specific validation (could all validation be moved to Core?)
│       ├── Utils/                      # Utility functions
│       │   ├── Canvas/                 # Canvas operations
│       │   ├── Graph/                  # Dependency analysis
│       │   ├── Introspection/          # Runtime inspection
│       │   └── [...]
│       └── GhJsonGrasshopper.cs        # Main façade entry point
│
├── tests/
│   ├── GhJSON.Core.Schema.Tests/
│   ├── GhJSON.Core.Serialization.Tests/
│   ├── GhJSON.Core.Deserialization.Tests/
│   ├── GhJSON.Core.Validation.Tests/
│   ├── GhJSON.Core.FixOperations.Tests/
│   ├── GhJSON.Core.MergeOperations.Tests/
│   ├── GhJSON.Core.TidyUpOperations.Tests/
│   ├── GhJSON.Core.SchemaMigration.Tests/
│   ├── GhJSON.Grasshopper.Serialization.Tests/
│   ├── GhJSON.Grasshopper.Deserialization.Tests/
│   ├── GhJSON.Grasshopper.Utils.Canvas.Tests/
│   ├── GhJSON.Grasshopper.Utils.Graph.Tests/
│   ├── GhJSON.Grasshopper.Utils.Introspection.Tests/
│   └── more tests...
│
└── docs/
```

---

## GhJSON.Core

**Purpose**: Platform-independent operations on GhJSON documents. No Grasshopper or Rhino dependencies.

### Core Validation Responsibilities

`GhJSON.Core` validation is responsible for:

- Schema validation (JSON Schema conformance), including extension schemas.
- Structural validation beyond schema, including:
  - Unique component IDs.
  - Connection reference integrity: all connection endpoint `id` values must reference existing components.

When both `id` and `instanceGuid` are present on a component, `id` is authoritative for internal references (connections, groups).

### Core API

```csharp
namespace GhJSON.Core
{
    /// <summary>
    /// Main entry point for GhJSON document operations.
    /// </summary>
    public static class GhJson
    {
        // Schema management
        public static string GetSchema(string version);
        public static MigrationResult MigrateSchema(GhJsonDocument doc, string targetVersion);
        public static GhJsonDocument MigrateSchema(GhJsonDocument doc, string targetVersion);

        // Document management  (immutable builders used like CreateComponentObject().WithId("123").WithName("MyComponent").Build())
        public static GhJsonDocument CreateDocument();
        public static GhJsonSchema CreateSchemaProperty();                              // Builder that creates the "schema" property of the document
        public static GhJsonMetadata CreateMetadataProperty();                          // Builder that creates the "documentMetadata" property of the schema
        public static GhJsonComponent CreateComponentObject();                          // Builder that creates a component object (componentData in schema)
        public static GhJsonComponentParameter CreateComponentParameterObject();        // Builder that creates a parameter object (parameterSettings in schema)
        public static GhJsonComponentState CreateComponentStateObject();                // Builder that creates a component state object (componentState in schema)
        public static GhJsonConnection CreateConnectionObject();                        // Builder that creates a connection object (connectionData in schema)
        public static GhJsonConnectionEndpoint CreateConnectionEndpointObject();        // Builder that creates a connection endpoint object (connectionEndpoint in schema)
        public static GhJsonGroup CreateGroupObject();                                  // Builder that creates a group object (groupData in schema)

        // Input/Output
        public static GhJsonDocument FromFile(string path);
        public static GhJsonDocument FromStream(Stream stream);
        public static GhJsonDocument FromJson(string json);
        public static void ToFile(GhJsonDocument doc, string path, WriteOptions? options = null);
        public static string ToJson(GhJsonDocument doc, WriteOptions? options = null);
        
        // Validation (this only validates against the schema)
        public static ValidationResult Validate(GhJsonDocument doc, ValidationLevel level = ValidationLevel.Standard);
        public static ValidationResult Validate(string json, ValidationLevel level = ValidationLevel.Standard);
        public static bool IsValid(GhJsonDocument doc, ValidationLevel level = ValidationLevel.Standard);
        public static bool IsValid(string json, ValidationLevel level = ValidationLevel.Standard);
        
        // Fix
        public static FixResult Fix(GhJsonDocument doc, FixOptions? options = null);
        public static GhJsonDocument Fix(GhJsonDocument doc, FixOptions? options = null);
        public static FixResult FixMetadata(GhJsonDocument doc);
        public static GhJsonDocument FixMetadata(GhJsonDocument doc);
        public static FixResult AssignMissingIds(GhJsonDocument doc);
        public static GhJsonDocument AssignMissingIds(GhJsonDocument doc);
        public static FixResult ReassignIds(GhJsonDocument doc);
        public static GhJsonDocument ReassignIds(GhJsonDocument doc);
        public static FixResult GenerateMissingInstanceGuids(GhJsonDocument doc);
        public static GhJsonDocument GenerateMissingInstanceGuids(GhJsonDocument doc);
        public static FixResult RegenerateInstanceGuids(GhJsonDocument doc);
        public static GhJsonDocument RegenerateInstanceGuids(GhJsonDocument doc);
        
        // Merge
        public static MergeResult Merge(GhJsonDocument baseDoc, GhJsonDocument incomingDoc, MergeOptions? options = null);
        public static GhJsonDocument Merge(GhJsonDocument baseDoc, GhJsonDocument incomingDoc, MergeOptions? options = null);
    }
}
```

---

## GhJSON.Grasshopper

**Purpose**: Grasshopper-specific serialization and canvas operations. Requires Grasshopper references.

### Grasshopper Validation Responsibilities

`GhJSON.Grasshopper` validation is responsible for Grasshopper-specific semantic checks, including:

- Component existence in installed libraries (by `componentGuid` and/or `name`).
- Parameter matching rules (e.g., resolving `paramName`/`paramIndex` against the instantiated component).
- Type compatibility rules for connections (e.g., detecting obvious mismatches between source/target parameter types).

### Grasshopper API Endpoints

```csharp
namespace GhJSON.Grasshopper
{
    /// <summary>
    /// Main entry point for Grasshopper operations.
    /// </summary>
    public static class GhJsonGrasshopper
    {
        // Serialize (GH → GhJSON) (use immutable builders from GhJson.Core)
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_DocumentObject> objects,
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
        
        // Data Type Extensability
        public static void RegisterCustomDataTypeSerializer<T>(IDataTypeSerializer<T> serializer);
        public static void UnregisterCustomDataTypeSerializer<T>();
        public static IEnumerable<IDataTypeSerializer> GetRegisteredDataTypeSerializers();

        // Object Handler Extensability
        public static void RegisterCustomObjectHandler<T>(IObjectHandler<T> handler);
        public static void UnregisterCustomObjectHandler<T>();
        public static IEnumerable<IObjectHandler> GetRegisteredObjectHandlers();
    }
}
```

---

## Data Type Serialization

### Format Standard

All data types use a **prefix:value** format for explicit type identification. Every time that a value is serialized, use the Data Type Serializer:

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
    bool IsValid(string value);
}
```

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
    
    public bool IsValid(string value) => 
        value.StartsWith($"{Prefix}:") && value.Split(',').Length == 2;
}
```

## Object Serialization and Deserialization Process

The object serialization is designed to extend compatibility with new components. Thus, the processing logic is quite complex, but robust.

### Serialization process

1. The Serialize method receives a list of IGH_ActiveObject.
2. For each object, the ObjectHandlerOrchestrator checks all compatible handlers in ObjectHandlerRegistry and applies all them in the priority order.
3. For property value serialization, the ObjectHandlerOrchestrator checks for compatible DataTypeSerializers.
4. Subsequent handlers will never override properties set by previous handlers. This protection is ensured at ObjectHandlerOrchestrator level, not at IObjectHandler level.
5. For special components or specific data types, custom handlers can set additional properties in the componentState property of the component schema.

### Deserialization process

1. The Deserialize method receives a list of GhJsonComponent.
2. For each component, the ObjectHandlerOrchestrator checks all compatible handlers and applies all them in the priority order.
3. For property value deserialization, the ObjectHandlerOrchestrator checks for compatible DataTypeSerializers. This is easier than serialization, because all DataTypes use a prefix to identify the data type.
4. Subsequent handlers will never set again properties that were already set by previous handlers. This protection is ensured at ObjectHandlerOrchestrator level, not at IObjectHandler level.
5. For special components or specific data types, custom handlers can set additional properties to the IGH_DocumentObject from the componentState property of the component schema.

```csharp
public interface IObjectHandler
    {
        // Determines if this handler can process the given document object.
        bool CanHandle(IGH_DocumentObject obj);

        // Determines if this handler can process the given serialized component.
        bool CanHandle(GhJsonComponent component);

        // Serialization operation
        GhJsonComponent Serialize(IGH_DocumentObject obj);

        // Deserialization operation
        void Deserialize(GhJsonComponent component, IGH_DocumentObject obj);

        // Schema extension URL (defined if this handler adds a schema extension)
        string SchemaExtensionUrl { get; }
    }
```

```csharp
// Built in objects
ObjectHandlerRegistry.Register(new IdentificationHandler(), priority: 0);
ObjectHandlerRegistry.Register(new PivotHandler(), priority: 0);
ObjectHandlerRegistry.Register(new SelectedPropertyHandler(), priority: 0);
ObjectHandlerRegistry.Register(new LockedPropertyHandler(), priority: 0);
ObjectHandlerRegistry.Register(new HiddenPropertyHandler(), priority: 0);
ObjectHandlerRegistry.Register(new RuntimeMessagesHandler(), priority: 0);
ObjectHandlerRegistry.Register(new IOIdentificationHandler(), priority: 0);
ObjectHandlerRegistry.Register(new IOModifiersHandler(), priority: 0);
ObjectHandlerRegistry.Register(new InternalizedDataHandler(), priority: 0);

// Extension objects (their data is in the extensions property in componentState)
ObjectHandlerRegistry.Register(new NumberSliderHandler(), priority: 100); // Will serialize and deserialize number slider value, interval, and rounding
ObjectHandlerRegistry.Register(new PanelHandler(), priority: 100); // Will serialize and deserialize panel text, font, color, alignment, etc.
ObjectHandlerRegistry.Register(new ScribbleHandler(), priority: 100); // Will serialize and deserialize scribble data, size, etc.
ObjectHandlerRegistry.Register(new ValueListHandler(), priority: 100); // Will serialize and deserialize value list options, and selected options
```
