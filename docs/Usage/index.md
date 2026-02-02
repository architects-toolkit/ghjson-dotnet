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

// Validate and get result
var result = GhJson.Validate(doc);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}

// Quick check
bool isValid = GhJson.IsValid(doc);
```

## Working with Grasshopper

### Serializing from Canvas

```csharp
using GhJSON.Grasshopper;

// Serialize all objects on canvas
var doc = GhJsonGrasshopper.Get();

// Serialize selected objects only
var doc = GhJsonGrasshopper.GetSelected();

// Serialize specific objects by GUID
var doc = GhJsonGrasshopper.GetByGuids(guids);
```

### Placing on Canvas

```csharp
using GhJSON.Grasshopper;

// Place document on canvas
var result = GhJsonGrasshopper.Put(doc);

// With options
var result = GhJsonGrasshopper.Put(doc, new PutOptions
{
    StartPosition = new PointF(100, 100),
    ClearExisting = false
});
```

## Building Documents Programmatically

```csharp
using GhJSON.Core;

// Create a new document
var doc = GhJson.CreateDocument()
    .WithSchema("1.0")
    .WithMetadata(m => m
        .WithTitle("My Definition")
        .WithAuthor("John Doe")
        .WithDescription("A parametric design"))
    .Build();

// Add a component
var slider = GhJson.CreateComponentObject()
    .WithName("Number Slider")
    .WithId(1)
    .WithPivot("100,100")
    .WithComponentState(s => s
        .WithExtension("gh.numberslider", new { value = "5<0,10>" }))
    .Build();

// Add a connection
var connection = GhJson.CreateConnectionObject()
    .WithFrom(1, "Number")
    .WithTo(2, 0)  // Using paramIndex
    .Build();
```

## Custom Data Type Serializers

```csharp
using GhJSON.Grasshopper;

// Register a custom serializer
GhJsonGrasshopper.RegisterCustomDataTypeSerializer(new MyCustomSerializer());

// Unregister
GhJsonGrasshopper.UnregisterCustomDataTypeSerializer<MyCustomType>();
```

## Custom Object Handlers

```csharp
using GhJSON.Grasshopper;

// Register a custom handler for a component type
GhJsonGrasshopper.RegisterCustomObjectHandler(new MyComponentHandler());

// Unregister
GhJsonGrasshopper.UnregisterCustomObjectHandler<MyComponent>();
```

## See Also

- [Architecture](../ARCHITECTURE.md) - Detailed architecture documentation
- [GhJSON Specification](https://github.com/architects-toolkit/ghjson-spec) - Official format specification
