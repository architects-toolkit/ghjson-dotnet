# GhJSON.NET

[![Package Version](https://img.shields.io/badge/version-1.1.0-brightgreen?style=for-the-badge)](https://www.nuget.org/packages/GhJSON.Core)
[![Status](https://img.shields.io/badge/status-Stable-brightgreen?style=for-the-badge)](https://github.com/architects-toolkit/ghjson-dotnet)
[![Schema Version](https://img.shields.io/badge/schema-v1.0-blue?style=for-the-badge)](https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-white?style=for-the-badge)](LICENSE)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/architects-toolkit/ghjson-dotnet)

.NET implementation of the [GhJSON specification](https://architects-toolkit.github.io/ghjson-spec/) for Grasshopper definition serialization.

## Overview

GhJSON is a JSON-based format for representing [Grasshopper](https://discourse.mcneel.com/c/grasshopper) definitions. This library provides:

- **GhJSON.Core** â€” Platform-independent document model and operations (read, write, validate, fix, merge, migrate, diff/patch, name resolution, layout calculation)
- **GhJSON.Grasshopper** â€” Grasshopper integration (serialize from canvas, place on canvas, automatic layout, data type serializers, object handlers)

## Documentation

See the [documentation index](./docs/index.md) for detailed guides and architecture.

## Installation

```bash
# Core library (platform-independent)
dotnet add package GhJSON.Core

# Grasshopper integration (requires Rhino 8+)
dotnet add package GhJSON.Grasshopper
```

## Quick Start

### Serialization (Grasshopper Canvas â†’ GhJSON)

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

// Serialize all objects from the active canvas
var document = GhJsonGrasshopper.Get();
string json = GhJson.ToJson(document);

// Or serialize only selected objects
var selected = GhJsonGrasshopper.GetSelected();
```

### Deserialization (GhJSON â†’ Grasshopper Canvas)

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

// Parse JSON and place on the active canvas
var document = GhJson.FromJson(json);
var result = GhJsonGrasshopper.Put(document);
```

### Validation

```csharp
using GhJSON.Core;

// Standard validation (offline, current schema version)
var result = GhJson.Validate(json);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error.Message);
    }
}

// Prefer online schema; falls back to embedded on failure
var result = GhJson.Validate(json, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Async validation
var result = await GhJson.ValidateAsync(json, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Or quick check
bool isValid = GhJson.IsValid(json);
bool isValid = await GhJson.IsValidAsync(json, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Patch validation
var patchResult = GhJson.ValidatePatch(patchJson);
var patchResult = GhJson.ValidatePatch(patchJson, preferOnline: true, schemaVersion: "1.0");
```

### Building Documents Programmatically

```csharp
using GhJSON.Core;
using GhJSON.Core.SchemaModels;

var slider = new GhJsonComponent
{
    Name = "Number Slider",
    Id = 1,
    Pivot = new GhJsonPivot(100, 100),
};

var doc = GhJson.CreateDocumentBuilder()
    .AddComponent(slider)
    .Build();
```

## Supported Features

### Core Operations (GhJSON.Core)

- **Read/Write** â€” Parse GhJSON from string, file, or stream; serialize back to JSON or file
- **Document Builder** â€” Fluent API for programmatic document construction
- **Validation** â€” Schema conformance, structural integrity (unique IDs, connection references, group membership), with online/offline schema loading and version selection
- **Fix** â€” Auto-repair metadata, assign missing IDs, regenerate instance GUIDs
- **Merge** â€” Combine two GhJSON documents with configurable conflict resolution
- **Schema Migration** â€” Migrate documents between schema versions
- **Diff/Patch** â€” Compare documents and apply `.ghpatch` changes with conflict tracking
- **Name Resolution** â€” Fuzzy matching for component and parameter names (alias dictionaries + Levenshtein distance)

### Grasshopper Integration (GhJSON.Grasshopper)

- **Serialize** â€” Read objects from the Grasshopper canvas into GhJSON documents
- **Deserialize** â€” Instantiate Grasshopper objects from GhJSON (without placing on canvas)
- **Get** â€” Query the active canvas with filters (selection, GUIDs, viewport)
- **Put** â€” Place GhJSON documents on the canvas with positioning and connection wiring
- **Layout** â€” Automatic component positioning with dependency-graph layout and collision-aware refinements
- **Delete** â€” Remove objects from the canvas by GUID with batch undo support
- **Query** â€” Fluent `CanvasSelector` API with viewport, GUID, type, category, and attribute filters
- **Grasshopper Validation** â€” Component existence checks, parameter matching, type compatibility

### Supported Data Types

All data types use a `prefix:value` string format for explicit type identification. The following serializers are registered by default:

| Type | Prefix | Format | Example |
|------|--------|--------|---------|
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

Custom data type serializers can be registered via `DataTypeRegistry.Register()`.

### Supported Component Handlers

Object handlers serialize and deserialize component-specific properties via the `componentState.extensions` mechanism:

| Handler | Extension Key | Description |
|---------|---------------|-------------|
| Number Slider | `gh.numberslider` | Slider value and rounding mode |
| Panel | `gh.panel` | Text, font, alignment, bounds, multiline, wrap settings |
| Scribble | `gh.scribble` | Scribble text, font, corners |
| Value List | `gh.valuelist` | List items, list mode, selected indices |
| Button | `gh.button` | Button state |
| Toggle | `gh.toggle` | Toggle state |
| Colour Swatch | `gh.colourswatch` | Colour swatch value |
| C# Script | `gh.csharp` | Script code, marshalling options |
| Python 3 Script | `gh.python` | Script code, marshalling options |
| IronPython Script | `gh.ironpython` | Script code, marshalling options |
| GhPython Script | `gh.ghpython` | Legacy GhPython script code |
| VB Script | `gh.vbscript` | VB script code sections (imports, script, additional) |
| SmartHopper | `smarthopper.state` | Selected AI provider name and canvas object selections |

Core handlers (applied to all objects):

- **IdentificationHandler** â€” Component name, GUID, instance GUID
- **PivotHandler** â€” Canvas position
- **SelectedPropertyHandler** â€” Selection state
- **LockedPropertyHandler** â€” Lock (disabled) state
- **HiddenPropertyHandler** â€” Preview visibility state
- **RuntimeMessagesHandler** â€” Error, warning, remark messages
- **IOIdentificationHandler** â€” Input/output parameter identification
- **IOModifiersHandler** â€” Data mapping, expressions, reverse, simplify, invert, reparameterize
- **InternalizedDataHandler** â€” Persistent/internalized parameter data

Custom object handlers can be registered via `ObjectHandlerRegistry.Register()`.

## GhJSON Format

See the [GhJSON Specification](https://architects-toolkit.github.io/ghjson-spec/) or the [ghjson-spec repo](https://github.com/architects-toolkit/ghjson-spec) for the complete format definition.

### Example

```json
{
  "schema": "1.0",
  "components": [
    {
      "name": "Number Slider",
      "componentGuid": "57da07bd-ecab-415d-9d86-af36d7073abc",
      "instanceGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "id": 1,
      "pivot": "100,200",
      "componentState": {
        "extensions": {
          "gh.numberslider": {
            "value": "5<0~10>"
          }
        }
      }
    }
  ],
  "connections": []
}
```

## License

Apache-2.0

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Related Projects

- [ghjson-spec](https://github.com/architects-toolkit/ghjson-spec) â€” GhJSON format specification
- [SmartHopper](https://github.com/architects-toolkit/SmartHopper) â€” AI-powered Grasshopper plugin (uses GhJSON)
