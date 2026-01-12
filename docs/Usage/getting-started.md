# Getting started

## Installation

```bash
# Core library (platform-independent)
dotnet add package GhJSON.Core

# Grasshopper integration (requires Rhino / Grasshopper runtime)
dotnet add package GhJSON.Grasshopper
```

## What to use when

- **If you only need to read/write/validate/manipulate `.ghjson` in a console/server app**:
  - Use `GhJSON.Core`.
- **If you need to serialize from (or place into) a running Grasshopper canvas**:
  - Use `GhJSON.Grasshopper`.

## Minimal example (GhJSON.Core)

```csharp
using GhJSON.Core;
using GhJSON.Core.Models.Document;

// Parse JSON
GhJsonDocument doc = GhJson.Parse(json);

// Validate
var validation = GhJson.Validate(doc);
if (!validation.IsValid)
{
    // validation.Errors / validation.Warnings
}

// Write back to JSON
string serialized = GhJson.Serialize(doc);
```

## Minimal example (GhJSON.Grasshopper)

These APIs must run inside Rhino/Grasshopper (they use `Grasshopper.Instances`).

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.Serialization;

// Read selected objects from the active canvas
var doc = GhJsonGrasshopper.GetSelected(SerializationOptions.Optimized);

// Place a document back into the active canvas
GhJsonGrasshopper.Put(doc);
```

## Notes

- `GhJsonGrasshopper.Deserialize(...)` **creates objects but does not place them**. Use `GhJsonGrasshopper.Put(...)` for the typical end-to-end “create on canvas” workflow.
- When targeting AI workflows, prefer `SerializationOptions.Optimized` to reduce token size (persistent data is omitted).
