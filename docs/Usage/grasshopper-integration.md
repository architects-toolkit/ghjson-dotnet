# GhJSON.Grasshopper (Rhino/Grasshopper integration)

`GhJSON.Grasshopper` is designed to run inside Rhino/Grasshopper (it references Grasshopper and RhinoCommon).

## Main entry point: `GhJSON.Grasshopper.GhJsonGrasshopper`

### Initialize

`GhJsonGrasshopper.Serialize(...)` / `Deserialize(...)` / `Get(...)` / `Put(...)` call `GhJsonGrasshopper.Initialize()` automatically.

Initialization ensures:

- Geometric type serializers are registered
- Built-in component handlers are available

### Serialize (Grasshopper → GhJSON)

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Serialization;

var doc = GhJsonGrasshopper.GetSelected(SerializationOptions.Standard);
string json = doc.ToJson();
```

`SerializationOptions` presets:

- `SerializationOptions.Standard`
  - Includes metadata, component state, parameter settings, schema properties, groups, connections, and persistent data.
- `SerializationOptions.Optimized`
  - Like Standard, but omits persistent data to reduce output size.
- `SerializationOptions.Lite`
  - Minimal output.

### Deserialize (GhJSON → Grasshopper objects)

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Serialization;

var doc = GhJSON.Core.GhJson.Parse(json);
var result = GhJsonGrasshopper.Deserialize(doc, DeserializationOptions.ComponentsOnly);

// result.Components contains instantiated IGH_DocumentObject objects
```

Important:

- Deserialization **creates instances** but does not place them on the canvas.
- Pivots/positions and wires are handled by the canvas utilities.

### Put (GhJSON → canvas)

Use `Put(...)` for the typical “place this definition on the active canvas” operation.

```csharp
using GhJSON.Grasshopper;

var put = GhJsonGrasshopper.Put(doc);
if (!put.IsSuccess)
{
    // put.Errors
}
```

`PutOptions` highlights:

- `Offset` (optional): shifts all pivots by a given amount
- `Spacing`, `UseExactPositions`, `UseDependencyLayout`
- `CreateConnections`, `CreateGroups`
- `PreserveInstanceGuids` (off by default)

## Grasshopper-specific validation

`GhJsonGrasshopper.Validate(...)` extends core validation with:

- Component existence checks (is a given `componentGuid` available on this machine)
- Best-effort type compatibility warnings for connections

```csharp
using GhJSON.Grasshopper;

var validation = GhJsonGrasshopper.Validate(json);
```

## Advanced: staged placement

If you need full control, you can:

1. `Deserialize(...)` to get instantiated objects
2. Use canvas utilities directly (placement / connections / groups)

The façade `Put(...)` already implements the common orchestration.
