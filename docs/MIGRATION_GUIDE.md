# Migration Guide: SmartHopper to ghjson-dotnet Parity

This guide documents the architectural changes made to `ghjson-dotnet` to achieve full feature parity with SmartHopper's GhJSON implementation.

## Overview

The `ghjson-dotnet` library has been enhanced to serve as the authoritative source for all GhJSON operations, enabling SmartHopper to delegate serialization, deserialization, validation, fixing, merging, and graph operations to this library.

## Key Architectural Changes

### 1. Staged Deserialization Flow

**Previous Behavior:**

- `GhJsonDeserializer.Deserialize()` created components, set pivots, and created connections in one operation

**New Behavior:**

- Deserialization creates components without positioning or wiring
- Returns `DeserializationResult` with `IdMapping` and `GuidMapping` for staged operations
- Positioning handled by `ComponentPlacer`
- Connection creation handled by `ConnectionManager.CreateConnections()`
- Group creation handled by `GroupManager.CreateGroups()`

**Migration Example:**

```csharp
// Old approach (not supported)
var result = GhJsonDeserializer.Deserialize(document, options);
// Components were already positioned and wired

// New approach (staged)
var result = GhJsonDeserializer.Deserialize(document, options);

// Stage 1: Position components
ComponentPlacer.PlaceComponents(result.Components, document, options);

// Stage 2: Create connections
ConnectionManager.CreateConnections(document.Connections, result.GuidMapping, canvas);

// Stage 3: Create groups
GroupManager.CreateGroups(document.Groups, result.GuidMapping, canvas);
```

### 2. Component Identification (GUID-First)

**Change:**

- Component lookup now prioritizes `ComponentGuid` over `Name`
- Avoids ambiguous matches when multiple components share the same name

**Implementation:**

```csharp
// Prefer GUID-first lookup
if (props.ComponentGuid != Guid.Empty)
{
    proxy = Instances.ComponentServer.EmitObjectProxy(props.ComponentGuid);
}

// Fallback to name-based lookup
if (proxy == null)
{
    proxy = Instances.ComponentServer.FindObjectByName(props.Name, true, true);
}
```

### 3. NickName-Based Connection Matching

**Previous Behavior:**

- Connections matched parameters by `Name` only

**New Behavior:**

- Serializer emits `NickName` (fallback to `Name`) in connection endpoints
- Connection resolution follows priority: `ParamIndex` → `NickName` → `Name`
- Robust against parameter display name changes

**Serialization:**

```csharp
var fromParamName = !string.IsNullOrEmpty(source.NickName) 
    ? source.NickName 
    : source.Name;
```

**Deserialization:**

```csharp
// Try by index first
if (connection.From.ParamIndex.HasValue)
{
    sourceParam = component.Params.Output[connection.From.ParamIndex.Value];
}

// Then by NickName
if (sourceParam == null)
{
    sourceParam = component.Params.Output.Find(p => p.NickName == connection.From.ParamName);
}

// Finally by Name
if (sourceParam == null)
{
    sourceParam = component.Params.Output.Find(p => p.Name == connection.From.ParamName);
}
```

### 4. Full Script Component Support

#### Script Code Application

**C#/Python/IronPython:**

- Script code applied via `Text` or `Script` property (reflection)
- Optional type hint injection replaces `object` parameters with typed equivalents

**VB.NET:**

- Three-section code support via `ScriptSource` reflection:
  - `UsingCode` (Imports)
  - `ScriptCode` (Main script)
  - `AdditionalCode` (Helper functions)

#### Parameter Rebuild

Script parameters are rebuilt from `InputSettings`/`OutputSettings`:

```csharp
// Clear default parameters
component.Params.Input.Clear();

// Create script-aware parameters
for (int i = 0; i < props.InputSettings.Count; i++)
{
    var settings = props.InputSettings[i];
    var param = ScriptParameterMapper.CreateParameter(
        settings, 
        "input", 
        lang, 
        isOutput: false
    );
    
    component.Params.RegisterInputParam(param);
    
    // Re-apply settings after registration
    ScriptParameterMapper.ApplySettings(param, settings);
    
    // Apply type hints
    if (!string.IsNullOrEmpty(settings.TypeHint))
    {
        ScriptParameterMapper.ApplyTypeHintToParameter(param, settings.TypeHint);
    }
}
```

#### Standard Output Parameter

The "out" parameter visibility is controlled via `ShowStandardOutput`:

```csharp
// Toggle to force parameter maintenance
if (current == desired)
{
    component.UsingStandardOutputParam = !desired;
    component.VariableParameterMaintenance();
}

// Set final state
component.UsingStandardOutputParam = desired;
component.VariableParameterMaintenance();
```

#### Script Parameter Extraction

Script-aware extraction captures variable names, type hints, and Optional flags:

```csharp
// Use script-aware extraction for script components
if (ScriptComponentFactory.IsScriptComponent(component))
{
    props.InputSettings = ExtractScriptParameterSettings(
        component.Params.Input, 
        component
    );
}
```

### 5. Instance GUID Handling

**New Feature:**

- `GhJsonFixer.InjectMissingInstanceGuids()` generates GUIDs for components without them
- Useful for AI-generated documents (e.g., `gh_generate`) that only provide integer IDs

**Usage:**

```csharp
// Fix document with missing GUIDs
var (fixedJson, idMapping) = GhJsonFixer.FixAll(
    json, 
    populateMetadata: true, 
    injectMissingGuids: true
);
```

### 6. Persistent Data Semantics

**Change:**

- Removed `ParameterSettings.PersistentData` extraction
- Persistent/volatile data handled via `SchemaProperties` and `PropertyManagerV2`

**Rationale:**

- Aligns with SmartHopper's approach
- Separates runtime data from parameter configuration

## Breaking Changes Summary

1. **Deserialization API:**
   - `GhJsonDeserializer.Deserialize()` no longer creates connections or sets pivots
   - Use staged approach with `ComponentPlacer`, `ConnectionManager`, and `GroupManager`

2. **Connection Matching:**
   - Changed from `Name`-only to `ParamIndex` → `NickName` → `Name` priority
   - Serialized connections now include `NickName`

3. **Persistent Data:**
   - `ParameterSettings.PersistentData` removed
   - Use `SchemaProperties` for persistent data handling

## New Capabilities

1. **Script Component Parity:**
   - Full reflection-based parameter handling
   - VB.NET 3-section code support
   - Type hint injection and application
   - Standard output parameter control

2. **Flexible GUID Handling:**
   - Optional GUID injection for AI-generated documents
   - Supports documents with only integer IDs

3. **Robust Connection Matching:**
   - NickName-based matching survives parameter renames
   - Fallback chain ensures maximum compatibility

## Testing Recommendations

When migrating to the new API:

1. **Test script components thoroughly:**
   - Verify parameter rebuild preserves variable names and type hints
   - Test VB.NET 3-section code roundtrip
   - Confirm standard output parameter visibility

2. **Validate connection robustness:**
   - Rename parameter nicknames and verify connections persist
   - Test with documents containing duplicate component names

3. **Test GUID-less documents:**
   - Verify `InjectMissingInstanceGuids()` generates valid GUIDs
   - Confirm staged placement works with injected GUIDs

## Support

For questions or issues related to this migration, please refer to:

- [CHANGELOG.md](../CHANGELOG.md) for detailed change history
- [API Documentation](./API.md) for usage examples
- GitHub Issues for bug reports and feature requests
