# GhJSON.NET Usage Guide

This guide covers common usage patterns for the ghjson-dotnet library.

## Quick Start

### Reading a GhJSON Document

```csharp
using GhJSON.Core;

// From file
var doc = GhJson.FromFile("path/to/definition.ghjson");

// From JSON string
var doc = GhJson.FromJson(jsonString);

// From stream
using var stream = File.OpenRead("path/to/definition.ghjson");
var doc = GhJson.FromStream(stream);
```

### Writing a GhJSON Document

```csharp
using GhJSON.Core;
using GhJSON.Core.Serialization;

// To file
GhJson.ToFile(doc, "path/to/output.ghjson");

// To JSON string
string json = GhJson.ToJson(doc);

// With options
string json = GhJson.ToJson(doc, new WriteOptions { Indented = true });
```

### Validating a Document

```csharp
using GhJSON.Core;
using GhJSON.Core.Validation;

// Standard validation (offline, current schema version)
var result = GhJson.Validate(doc);

// Prefer online schema; falls back to embedded on failure
var result = GhJson.Validate(json, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Async validation
var result = await GhJson.ValidateAsync(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at {error.Path}: {error.Message}");
    }

    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"Warning: {warning.Message}");
    }
}

// Quick check
bool isValid = GhJson.IsValid(doc);
bool isValid = await GhJson.IsValidAsync(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Quick check with message
if (!GhJson.IsValid(json, out string? message))
{
    Console.WriteLine(message);
}
```

### Validation Levels

```csharp
// Minimal - basic JSON structure only
GhJson.Validate(doc, ValidationLevel.Minimal);

// Standard (default) - schema conformance + structural checks
GhJson.Validate(doc, ValidationLevel.Standard);

// Strict - all checks plus additional semantic validation
GhJson.Validate(doc, ValidationLevel.Strict);
```

### Schema Version and Online Loading

```csharp
// Prefer online schema with automatic fallback to embedded on failure
var result = GhJson.Validate(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true);

// Async with cancellation token
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await GhJson.ValidateAsync(doc, ValidationLevel.Standard, schemaVersion: "1.0", preferOnline: true, cts.Token);
```

## Working with Grasshopper

### Serializing from Canvas

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.GetOperations;

// Serialize all objects on canvas
var doc = GhJsonGrasshopper.Get();

// Serialize selected objects only
var doc = GhJsonGrasshopper.GetSelected();

// Serialize specific objects by GUID
var doc = GhJsonGrasshopper.GetByGuids(guids);

// With options
var doc = GhJsonGrasshopper.Get(new GetOptions
{
    IncludeConnections = true,
    IncludeGroups = true,
    IncludeInternalizedData = true,
    IncludeRuntimeMessages = false,
    IncludeMetadata = true,
    MetadataAuthor = "Jane Smith",
});
```

### Placing on Canvas

```csharp
using GhJSON.Grasshopper;
using GhJSON.Grasshopper.PutOperations;

// Place document on canvas
var result = GhJsonGrasshopper.Put(doc);

// With options
var result = GhJsonGrasshopper.Put(doc, new PutOptions
{
    Offset = new PointF(100, 100),
    CreateConnections = true,
    CreateGroups = true,
    SelectPlacedObjects = true,
    RegenerateInstanceGuids = true,
    SkipInvalidComponents = true,
});
```

### Querying the Canvas

```csharp
using GhJSON.Grasshopper;

// Create a fluent selector over the active canvas
var selector = GhJsonGrasshopper.Select();

// Or over a specific GH_Document
var selector = GhJsonGrasshopper.Select(ghDocument);

// Or over an explicit set of objects
var selector = GhJsonGrasshopper.Select(objects);
```

### Serializing Arbitrary Objects

```csharp
using GhJSON.Grasshopper;

// Serialize a list of IGH_DocumentObject directly
var doc = GhJsonGrasshopper.Serialize(objects);

// With options
var doc = GhJsonGrasshopper.Serialize(objects, new SerializationOptions
{
    IncludeConnections = true,
    IncludeGroups = true,
    IncludeMetadata = false,
});
```

## Building Documents Programmatically

The `GhJson` facade provides factory methods for creating schema model objects.
Schema models (`GhJsonComponent`, `GhJsonConnection`, etc.) are mutable POCOs
that you populate directly. Use `DocumentBuilder` for immutable document assembly.

```csharp
using GhJSON.Core;
using GhJSON.Core.SchemaModels;

// Create components
var slider = new GhJsonComponent
{
    Name = "Number Slider",
    Id = 1,
    Pivot = new GhJsonPivot(100, 100),
};

var addition = new GhJsonComponent
{
    Name = "Addition",
    Id = 2,
    Pivot = new GhJsonPivot(300, 100),
};

// Create a connection
var connection = new GhJsonConnection
{
    From = new GhJsonConnectionEndpoint { Id = 1, ParamName = "Number" },
    To = new GhJsonConnectionEndpoint { Id = 2, ParamIndex = 0 },
};

// Build the document (validates on Build)
var doc = GhJson.CreateDocumentBuilder()
    .AddComponent(slider)
    .AddComponent(addition)
    .AddConnection(connection)
    .Build();
```

## Fix Operations

```csharp
using GhJSON.Core;
using GhJSON.Core.FixOperations;

// Apply all default fixes
var fixResult = GhJson.Fix(doc);
var fixedDoc = fixResult.Document;

// With options
var fixResult = GhJson.Fix(doc, new FixOptions
{
    AssignMissingIds = true,
    GenerateMissingInstanceGuids = true,
    FixMetadata = true,
    RemoveInvalidConnections = true,
});

// Individual fix operations
var result = GhJson.AssignMissingIds(doc);
var result = GhJson.ReassignIds(doc);
var result = GhJson.GenerateMissingInstanceGuids(doc);
var result = GhJson.RegenerateInstanceGuids(doc);
var result = GhJson.FixMetadata(doc);
```

## Merge Operations

```csharp
using GhJSON.Core;

var mergeResult = GhJson.Merge(baseDoc, incomingDoc);
var mergedDoc = mergeResult.Document;
```

## Diff and Patch Operations

The library implements the `.ghpatch` sibling profile from
[ghjson-spec](https://architects-toolkit.github.io/ghjson-spec/). Patches are
semantic per-entity descriptions of additions, removals and modifications keyed
by GhJSON identity (`instanceGuid` > `id` > structural fingerprint).

```csharp
using GhJSON.Core;
using GhJSON.Core.DiffOperations;
using GhJSON.Core.PatchModels;

// Generate a diff between two documents
var diffResult = GhJson.Diff(left, right);
GhPatchDocument patch = diffResult.Patch;

Console.WriteLine($"Components ops: {diffResult.ComponentOpCount}, " +
                  $"connections ops: {diffResult.ConnectionOpCount}, " +
                  $"groups ops: {diffResult.GroupOpCount}");

// Serialize the patch to a `.ghpatch` file
GhJson.PatchToFile(patch, "edit.ghpatch");

// Load a `.ghpatch` file and apply it to a base document
var loaded = GhJson.PatchFromFile("edit.ghpatch");
var applyResult = GhJson.ApplyPatch(left, loaded);

if (applyResult.Success && !applyResult.HasConflicts)
{
    // Round-trip: applyResult.Document is semantically equal to `right`.
}
else
{
    foreach (var c in applyResult.Conflicts)
    {
        Console.WriteLine($"[{c.Kind}] {c.Message} ({c.Path})");
    }
}
```

### Diff Options

```csharp
var options = new DiffOptions
{
    IgnoreRuntimeMessages = true,    // errors / warnings / remarks on components
    IgnoreMetadataCounters = true,   // metadata.componentCount, connectionCount, groupCount
    IgnoreMetadataTimestamps = true, // metadata.created, modified
    IgnorePivots = false,            // canvas positions are diffed by default
    IncludeBaseChecksum = true,      // emit sha256 in patch.base for verification
};

var diff = GhJson.Diff(left, right, options);
```

### Apply Options

```csharp
var options = new ApplyPatchOptions
{
    VerifyBase = true,                // refuse to apply on base checksum mismatch
    ContinueOnConflict = true,        // collect all conflicts, don't throw on first
    RenumberCollidingAddedIds = true, // reassign component ids on collision
};

var apply = GhJson.ApplyPatch(baseDoc, patch, options);
```

### Patch Validation

```csharp
// Offline (embedded ghpatch.schema.json)
var result = GhJson.ValidatePatch(patch);

// With online validation and explicit schema version
var result = GhJson.ValidatePatch(patch, preferOnline: true, schemaVersion: "1.0");

// From raw JSON string
var result = GhJson.ValidatePatch(patchJson, preferOnline: false, schemaVersion: "1.0");

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Path}: {error.Message}");
    }
}
```

## Schema Migration

```csharp
using GhJSON.Core;

// Check if migration is needed
if (GhJson.NeedsMigration(doc))
{
    var migrationResult = GhJson.MigrateSchema(doc);
    var migratedDoc = migrationResult.Document;
}
```

## Custom Data Type Serializers

```csharp
using GhJSON.Grasshopper;

// Register a custom serializer
GhJsonGrasshopper.RegisterCustomDataTypeSerializer(new MyCustomSerializer());

// Unregister
GhJsonGrasshopper.UnregisterCustomDataTypeSerializer<MyCustomType>();

// List all registered serializers
var serializers = GhJsonGrasshopper.GetRegisteredDataTypeSerializers();
```

## Custom Object Handlers

```csharp
using GhJSON.Grasshopper;

// Register a custom handler for a component type
var handler = new MyComponentHandler();
GhJsonGrasshopper.RegisterCustomObjectHandler(handler);

// Unregister
GhJsonGrasshopper.UnregisterCustomObjectHandler(handler);

// List all registered handlers
var handlers = GhJsonGrasshopper.GetRegisteredObjectHandlers();
```

## See Also

- [Metadata](./metadata.md) - Document metadata guide
- [Architecture](../ARCHITECTURE.md) - Detailed architecture documentation
- [GhJSON Specification](https://github.com/architects-toolkit/ghjson-spec) - Official format specification
