# Document Metadata

GhJSON documents can include optional metadata that provides information about the definition, its origin, and statistics.

## Enabling Metadata

Metadata is **disabled by default**. To include metadata in the output, set `IncludeMetadata = true`:

```csharp
// Using GetOptions (canvas operations)
var options = new GetOptions { IncludeMetadata = true };
var doc = GhJsonGrasshopper.Get(options);

// Using SerializationOptions (direct serialization)
var options = new SerializationOptions { IncludeMetadata = true };
var doc = GhJsonGrasshopper.Serialize(objects, options);
```

## Auto-Populated Properties

When metadata is enabled, the following properties are automatically populated:

| Property | Source | Description |
|----------|--------|-------------|
| `title` | Document file name | Extracted from `GH_Document.FilePath` (without extension) |
| `modified` | Current timestamp | UTC timestamp when the GhJSON was generated |
| `rhinoVersion` | `Rhino.RhinoApp.Version` | The running Rhino version |
| `grasshopperVersion` | `Grasshopper.Versioning.Version` | The running Grasshopper version |
| `componentCount` | Component list | Number of serialized components |
| `connectionCount` | Connection list | Number of extracted connections |
| `groupCount` | Group list | Number of extracted groups |
| `dependencies` | Component assemblies | List of non-standard plugin assemblies |
| `generatorName` | Default: `"ghjson-dotnet"` | Name of the generating tool |
| `generatorVersion` | Assembly version | Version of the ghjson-dotnet library |

## User-Provided Overrides

You can override or supplement the auto-populated values using the metadata override properties:

```csharp
var options = new GetOptions
{
    IncludeMetadata = true,
    
    // Override auto-populated values
    MetadataTitle = "My Parametric Facade",
    MetadataGeneratorName = "MyCustomTool",
    MetadataGeneratorVersion = "2.0.0",
    
    // Add user-only values (not auto-populated)
    MetadataDescription = "A parametric facade definition for curtain walls",
    MetadataVersion = "1.2.0",
    MetadataAuthor = "John Doe",
    MetadataTags = new List<string> { "facade", "parametric", "curtain-wall" }
};

var doc = GhJsonGrasshopper.Get(options);
```

### Override Properties

| Property | Type | Behavior |
|----------|------|----------|
| `MetadataTitle` | `string?` | Overrides auto-populated title from file name |
| `MetadataDescription` | `string?` | User-provided only (no auto-population) |
| `MetadataVersion` | `string?` | User-provided only (definition version, not schema version) |
| `MetadataAuthor` | `string?` | User-provided only |
| `MetadataTags` | `List<string>?` | User-provided only |
| `MetadataGeneratorName` | `string?` | Overrides default `"ghjson-dotnet"` |
| `MetadataGeneratorVersion` | `string?` | Overrides auto-populated assembly version |

## Example Output

```json
{
  "schema": "1.0",
  "metadata": {
    "title": "My Parametric Facade",
    "description": "A parametric facade definition for curtain walls",
    "version": "1.2.0",
    "author": "John Doe",
    "modified": "2026-02-07T19:30:00.0000000Z",
    "rhinoVersion": "8.26.25349.19001",
    "grasshopperVersion": "1.0.0007",
    "tags": ["facade", "parametric", "curtain-wall"],
    "dependencies": ["Kangaroo2Component", "LunchBox"],
    "componentCount": 42,
    "connectionCount": 58,
    "groupCount": 5,
    "generatorName": "MyCustomTool",
    "generatorVersion": "2.0.0"
  },
  "components": [...]
}
```

## Metadata Schema Reference

All metadata properties are optional. The full schema is defined in the [GhJSON specification](https://github.com/architects-toolkit/ghjson-spec).

| Property | Type | Description |
|----------|------|-------------|
| `title` | string | The title of the definition |
| `description` | string | A description of what this definition does |
| `version` | string | Version of the definition itself (incremented on each save) |
| `author` | string | The author of this definition |
| `created` | string | Creation timestamp in ISO 8601 format |
| `modified` | string | Last modification timestamp in ISO 8601 format |
| `rhinoVersion` | string | The Rhino version this definition was created with |
| `grasshopperVersion` | string | The Grasshopper version this definition was created with |
| `tags` | string[] | List of tags for categorizing and searching definitions |
| `dependencies` | string[] | List of required plugin dependencies |
| `componentCount` | integer | Total number of components in the document |
| `connectionCount` | integer | Total number of connections in the document |
| `groupCount` | integer | Total number of groups in the document |
| `generatorName` | string | Name of the tool that generated this GhJSON file |
| `generatorVersion` | string | Version of the tool that generated this file |
| `extensions` | object | Extension point for metadata produced by object handlers |

## Architecture

The metadata building logic is encapsulated in the `MetadataBuilder` class (`GhJSON.Grasshopper.GetOperations.MetadataBuilder`), which:

1. **Applies user overrides** from `GetOptions` or `SerializationOptions`
2. **Extracts version info** from Rhino and Grasshopper APIs
3. **Calculates counts** from the serialized components, connections, and groups
4. **Applies generator info** with fallback to assembly version
5. **Collects dependencies** by scanning component assemblies for non-standard plugins
