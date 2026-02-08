# GhJSON.NET

[![Package Version](https://img.shields.io/badge/version-1.0.0-blue)](https://www.nuget.org/packages/GhJSON.Core/1.0.0)
[![Schema Version](https://img.shields.io/badge/schema-v1.0-blue)](https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-green)](LICENSE)

.NET implementation of the [GhJSON specification](https://architects-toolkit.github.io/ghjson-spec/) for Grasshopper definition serialization.

## Overview

GhJSON is a JSON-based format for representing [Grasshopper](https://discourse.mcneel.com/c/grasshopper) definitions. This library provides:

- **GhJSON.Core** — Platform-independent document model and operations (read, write, validate, fix, merge, migrate)
- **GhJSON.Grasshopper** — Grasshopper integration (serialize from canvas, place on canvas, data type serializers, object handlers)

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

### Serialization (Grasshopper Canvas → GhJSON)

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

// Serialize all objects from the active canvas
var document = GhJsonGrasshopper.Get();
string json = GhJson.ToJson(document);

// Or serialize only selected objects
var selected = GhJsonGrasshopper.GetSelected();
```

### Deserialization (GhJSON → Grasshopper Canvas)

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

var result = GhJson.Validate(json);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error.Message);
    }
}

// Or quick check
bool isValid = GhJson.IsValid(json);
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

- [ghjson-spec](https://github.com/architects-toolkit/ghjson-spec) — GhJSON format specification
- [SmartHopper](https://github.com/architects-toolkit/SmartHopper) — AI-powered Grasshopper plugin (uses GhJSON)
