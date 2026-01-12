# GhJSON.NET

.NET implementation of the [GhJSON format](https://architects-toolkit.github.io/ghjson-spec/) for Grasshopper definition serialization.

## Overview

GhJSON is a JSON-based format for representing Grasshopper definitions. This library provides:

- **GhJSON.Core**: Platform-independent models and validation
- **GhJSON.Grasshopper**: Grasshopper integration for serialization/deserialization

## Documentation

See the documentation index at [`docs/index.md`](./docs/index.md).

## Installation

```bash
# Core library (platform-independent)
dotnet add package GhJSON.Core

# Grasshopper integration (requires Rhino 8+)
dotnet add package GhJSON.Grasshopper
```

## Quick Start

### Serialization (Grasshopper Canvas → GhJSON)

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Serialization;

// Serialize selected components from the active canvas
var document = GhJsonGrasshopper.GetSelected(SerializationOptions.Optimized);
var json = document.ToJson();
```

### Deserialization (GhJSON → Grasshopper Canvas)

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

// Parse and place on the active canvas
var document = GhJson.Parse(json);
var put = GhJsonGrasshopper.Put(document);
```

### Validation

```csharp
using GhJSON.Core;

var validationResult = GhJson.Validate(json);
if (!validationResult.IsValid)
{
    // validationResult.Errors / validationResult.Warnings
}
```

## GhJSON Format

See the [GhJSON Specification](https://architects-toolkit.github.io/ghjson-spec/) or the [GhJSON-spec repo](https://github.com/architects-toolkit/ghjson-spec) for the complete format definition.

### Example

```json
{
  "schemaVersion": "1.0",
  "components": [
    {
      "name": "Number Slider",
      "id": 1,
      "componentGuid": "57da07bd-ecab-415d-9d86-af36d7073abc",
      "instanceGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "pivot": "100,200",
      "componentState": {
        "value": "5<0,10>"
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
