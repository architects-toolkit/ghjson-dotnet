# GhJSON.Core (standalone / non-Grasshopper)

`GhJSON.Core` is the platform-independent package. It contains:

- The GhJSON document model (`GhJsonDocument`)
- JSON read/write helpers (`GhJson` facade)
- Validation (`GhJsonValidator`)
- Operations such as fix / migration / merge / tidy

## Main entry point: `GhJSON.Core.GhJson`

### Read / write

```csharp
using GhJSON.Core;

var doc = GhJson.Read("my-definition.ghjson");

// Write to a file
GhJson.Write(doc, "out.ghjson");

// Serialize to string
string json = GhJson.Serialize(doc);
```

### Validate

```csharp
using GhJSON.Core;

var result = GhJson.Validate(json);
if (!result.IsValid)
{
    foreach (var e in result.Errors)
    {
        // Handle errors
    }
}
```

### Fix / repair

Use this when you want to normalize or repair AI-generated documents.

```csharp
using GhJSON.Core;

var fix = GhJson.Fix(doc);
// fix.Document is the potentially updated document
// fix.Actions describes the applied operations
```

### Migrate

```csharp
using GhJSON.Core;

if (GhJson.NeedsMigration(json))
{
    var migrated = GhJson.Migrate(json);
    if (migrated.Success)
    {
        var migratedDoc = migrated.Document;
    }
}
```

## Model: `GhJsonDocument`

`GhJsonDocument` also provides convenience JSON helpers:

- `doc.ToJson()`
- `GhJsonDocument.FromJson(json)`

For library consumers, prefer using `GhJson` to keep serialization settings consistent (notably `CompactPositionConverter`).

## Recommended workflow in non-Grasshopper apps

- **Load / parse** with `GhJson.Read(...)` or `GhJson.Parse(...)`
- **Validate** with `GhJson.Validate(...)`
- **Repair** with `GhJson.Fix(...)` (optional but recommended for untrusted inputs)
- **Apply operations** (merge/tidy/migrate)
- **Write** with `GhJson.Write(...)`

## Security note (untrusted JSON)

`GhJSON.Core` expects JSON documents that match the GhJSON schema. Treat incoming `.ghjson` as untrusted input:

- Validate with `GhJson.Validate(...)`
- Consider using `GhJson.Fix(...)` to normalize missing IDs / GUIDs before downstream logic
