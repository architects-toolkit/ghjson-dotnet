# GhJSON.NET

[![Package Version](https://img.shields.io/badge/version-1.3.4-brightgreen?style=for-the-badge)](https://www.nuget.org/packages/GhJSON.Core)
[![Status](https://img.shields.io/badge/status-Stable-brightgreen?style=for-the-badge)](https://github.com/architects-toolkit/ghjson-dotnet)
[![Schema Version](https://img.shields.io/badge/schema-v1.0-blue?style=for-the-badge)](https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-white?style=for-the-badge)](LICENSE)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/architects-toolkit/ghjson-dotnet)

Official .NET implementation of the [GhJSON specification](https://architects-toolkit.github.io/ghjson-spec/) — a compact, human-readable JSON format for representing, diffing, and editing [Grasshopper 3D](https://discourse.mcneel.com/c/grasshopper) definitions.

## Packages

GhJSON.NET is split into two NuGet packages so you can work with `.ghjson` and `.ghpatch` files anywhere — inside Rhino or in a standalone .NET service, CLI, or AI pipeline:

| Package | Dependencies | What it does |
|---|---|---|
| **`GhJSON.Core`** | None (platform-independent .NET) | Document model, fluent builder, schema & structural validation, auto-fix, merge, diff & `.ghpatch` application, fuzzy name resolution, and dependency-graph layout. |
| **`GhJSON.Grasshopper`** | Rhino 8 / Grasshopper | Live canvas serialization (`Get`), placement and in-place updates (`Put`), fluent canvas queries (`CanvasSelector`), data-type serializers, and component state handlers. |

```bash
# Platform-independent core library (no Rhino/Grasshopper required)
dotnet add package GhJSON.Core

# Grasshopper canvas integration (requires Rhino 8+)
dotnet add package GhJSON.Grasshopper
```

## Quick start

### 1. Read the Grasshopper canvas to GhJSON

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

// Serialize the entire active canvas (or use GhJsonGrasshopper.GetSelected())
var document = GhJsonGrasshopper.Get();
string json = GhJson.ToJson(document, indented: true);
```

### 2. Place or update a GhJSON definition on the canvas

```csharp
using GhJSON.Core;
using GhJSON.Grasshopper;

var document = GhJson.FromJson(json);
var result = GhJsonGrasshopper.Put(document);
```

### 3. Build, validate, and auto-repair in pure .NET (no Rhino required)

```csharp
using GhJSON.Core;
using GhJSON.Core.SchemaModels;

// Build a document programmatically
var doc = GhJson.CreateDocumentBuilder()
    .AddComponent(new GhJsonComponent
    {
        Name = "Number Slider",
        Id = 1,
        Pivot = new GhJsonPivot(100, 100),
    })
    .Build();

// Validate against the embedded v1.0 schema (or pass preferOnline: true)
var validation = GhJson.Validate(doc);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"{error.Path}: {error.Message}");
    }
}

// Auto-fix missing IDs, metadata, or instance GUIDs
var fixedResult = GhJson.Fix(doc);
```

### 4. Diff two definitions and apply a `.ghpatch`

```csharp
using GhJSON.Core;

// Compute a structural diff between two GhJSON documents
var patch = GhJson.Diff(baseDoc, targetDoc);
string patchJson = GhJson.ToPatchJson(patch, indented: true);

// Validate and apply the patch to a base document
if (GhJson.ValidatePatch(patchJson).IsValid)
{
    var applyResult = GhJson.ApplyPatch(baseDoc, patch);
}
```

## Key capabilities

### Core operations (`GhJSON.Core`)

- **Read & Write** — Parse and format `.ghjson` and `.ghpatch` documents from strings, files, or streams.
- **Validation** — `Minimal`, `Standard`, and `Strict` validation levels covering JSON Schema conformance, unique IDs, valid wire endpoints, and group membership, with offline embedded schemas and optional online schema fetching.
- **Auto-Fix** — Repair metadata counts, assign missing sequential IDs, and regenerate colliding instance GUIDs.
- **Diff, Patch & Merge** — Generate `.ghpatch` diffs (matching components by `instanceGuid`, `id`, or structural fingerprint), apply patches with base-checksum verification, or merge documents with target-wins conflict resolution.
- **Fuzzy Name Resolution** — Resolve informal or slightly misspelled component and parameter names via alias dictionaries and Levenshtein distance.
- **Graph Layout** — Compute clean left-to-right canvas positions from the connection graph with collision-aware refinement.

### Grasshopper integration (`GhJSON.Grasshopper`)

- **Canvas Query (`Get` / `CanvasSelector`)** — Filter canvas objects by selection, instance GUIDs, viewport bounds, component type, category (`+Maths`, `-Script`), attributes (`error`, `warning`, `selected`, `enabled`, `previewon`), and upstream/downstream connection depth.
- **Canvas Placement (`Put`)** — Instantiate components, restore state and internalized values, wire connections, create groups, and update existing components in place by `instanceGuid` with single-step Grasshopper undo.
- **Prefixed Data Type Serializers** — 15 built-in `prefix:value` serializers (`text:`, `number:`, `integer:`, `boolean:`, `argb:`, `pointXYZ:`, `vectorXYZ:`, `line2p:`, `planeOXY:`, `circleCNRS:`, `arc3P:`, `boxOXY:`, `rectangleCXY:`, `interval:`, `bounds:`), extensible via `DataTypeRegistry.Register()`.
- **Component State & Extension Handlers** — Built-in handlers for Number Slider (`gh.numberslider`), Panel (`gh.panel`), Scribble (`gh.scribble`), Value List (`gh.valuelist`), Button (`gh.button`), Boolean Toggle (`gh.toggle`), Colour Swatch (`gh.colourswatch`), C# / Python 3 / IronPython / GhPython / VB Script components, and SmartHopper state (`smarthopper.state`), plus 9 universal core handlers (identification, pivot, selection, lock, preview, runtime messages, parameter I/O, modifiers, and internalized data). Extensible via `ObjectHandlerRegistry.Register()`.

## Documentation

- [**Documentation Index**](./docs/index.md) — Overview of all guides and references
- [**Usage Guide**](./docs/Usage/index.md) — Reading, writing, validation, fixing, diff/patch, merging, and canvas operations
  - [**Metadata**](./docs/Usage/metadata.md) — Document metadata options and auto-population
  - [**Name Resolution**](./docs/Usage/name-resolution.md) — Fuzzy matching for component and parameter names
- [**Architecture & Extensibility**](./docs/ARCHITECTURE.md) — Full API surface, data-type format table, extension handler schemas, and custom handler registration

## Related projects

- [**ghjson-spec**](https://github.com/architects-toolkit/ghjson-spec) — The GhJSON (`.ghjson`) and GhPatch (`.ghpatch`) format specifications and JSON Schemas
- [**ghjson-graph**](https://github.com/architects-toolkit/ghjson-graph) — Zero-dependency TypeScript library for parsing GhJSON documents into dependency graphs — stats, validation, and SVG topology thumbnails
- [**SmartHopper**](https://github.com/architects-toolkit/SmartHopper) — Open-source AI assistant and component plugin for Grasshopper 3D built on GhJSON

## Contributing & license

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines. Licensed under [Apache-2.0](LICENSE).
