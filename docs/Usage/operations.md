# Operations (fix / migrate / merge / tidy)

This page covers the operations that let you manipulate GhJSON documents outside of Grasshopper.

## Fix

Fix operations normalize documents (useful for AI-generated GhJSON).

```csharp
using GhJSON.Core;

var fix = GhJson.Fix(doc);
// fix.Actions describes what changed
```

If you only want IDs assigned:

```csharp
var fixMinimal = GhJson.FixMinimal(doc);
```

## Migrate

GhJSON documents have a `schemaVersion`. If you need to normalize legacy formats:

```csharp
using GhJSON.Core;

if (GhJson.NeedsMigration(json))
{
    var migrated = GhJson.Migrate(json);
    if (!migrated.Success)
    {
        // migrated.ErrorMessage
    }
}
```

## Merge

There are two merge APIs in this repo:

- `GhJSON.Core.Operations.MergeOperations.DocumentMerger` (configurable via `MergeOptions`)
- `GhJSON.Core.Serialization.GhJsonMerger` (simpler helper that also exposes ID remapping stats)

Typical configurable merge:

```csharp
using GhJSON.Core.Operations.MergeOperations;

var merger = new DocumentMerger(new MergeOptions
{
    ConflictResolution = ConflictResolution.TargetWins,
    AdjustPositions = true,
    ReassignIds = true,
    MergeGroups = true,
});

var result = merger.Merge(target, source);
```

## Tidy (layout)

Tidy operations can reorganize pivots to produce more readable layouts.

```csharp
using GhJSON.Core.Operations.TidyOperations;

var tidy = DocumentTidier.TidyAll(doc);
if (!tidy.Success)
{
    // tidy.ErrorMessage
}
```

Layout analysis without modifications:

```csharp
var analysis = DocumentTidier.AnalyzeLayout(doc);
```
