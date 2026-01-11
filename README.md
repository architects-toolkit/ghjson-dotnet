# GhJSON.NET

.NET implementation of the GhJSON format for Grasshopper definition serialization.

## Overview

GhJSON is a JSON-based format for representing Grasshopper definitions. This library provides:

- **GhJSON.Core**: Platform-independent models and validation
- **GhJSON.Grasshopper**: Grasshopper integration for serialization/deserialization

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

// Serialize selected components
var document = GhJsonSerializer.Serialize(selectedObjects, SerializationOptions.Standard);
string json = document.ToJson();
```

### Deserialization (GhJSON → Grasshopper Canvas)

```csharp
using GhJSON.Grasshopper;

// Deserialize from JSON
var document = GrasshopperDocument.FromJson(json);
var result = GhJsonDeserializer.Deserialize(document, DeserializationOptions.Standard);

// Place components on canvas
foreach (var component in result.Components)
{
    ghDocument.AddObject(component, false);
}
```

### Validation

```csharp
using GhJSON.Core.Validation;

var validationResult = GhJsonValidator.Validate(json);
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"[{error.Severity}] {error.Message}");
    }
}
```

## GhJSON Format

See the [GhJSON Specification](https://github.com/architects-toolkit/ghjson-spec) for the complete format definition.

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
