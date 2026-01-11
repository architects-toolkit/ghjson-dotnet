# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Core Features

- **Instance GUID injection**: Added `GhJsonFixer.InjectMissingInstanceGuids()` to generate GUIDs for components that don't have them (useful for AI-generated documents)
- **Optional GUID injection in FixAll**: Added `injectMissingGuids` parameter to `GhJsonFixer.FixAll()` to support documents without instanceGuids

#### Script Component Support

- **Full script component deserialization parity**: Comprehensive reflection-based script component handling
  - Script code application with optional C# type hint injection
  - Parameter rebuild from `InputSettings`/`OutputSettings` using reflection
  - VB.NET 3-section code support (Imports/Script/Additional)
  - Standard output parameter visibility control (`ShowStandardOutput`)
- **Script parameter creation**: Added `ScriptParameterMapper.CreateParameter()` for reflection-based script parameter instantiation
  - Creates `ScriptVariableParam` for C#/Python/IronPython
  - Creates `Param_ScriptVariable` for VB.NET
  - Applies `Optional` and `AllowTreeAccess` properties
  - Supports type hint application via `TypeHints.Select()`
- **Script parameter extraction**: Added `ScriptParameterMapper.ExtractSettings()` to capture script-specific parameter data
  - Extracts variable names, type hints, Optional flags
  - Preserves principal parameter marking
- **Script state serialization**: Enhanced serializer to capture script-specific state
  - VB.NET code serialized as `ComponentState.VBCode` (3 sections)
  - Standard output visibility serialized as `ComponentState.ShowStandardOutput`
  - Script-aware parameter extraction using `ScriptParameterMapper`

#### Serialization & Deserialization

- **Staged deserialization flow**: `GhJsonDeserializer` now creates components without positioning or connection creation
  - Exposes `IdMapping` and `GuidMapping` in `DeserializationResult` for staged operations
  - Positioning and connection creation delegated to canvas utilities
- **GUID-first component lookup**: Components resolved by GUID first, then by name (avoids ambiguous name matches)
- **NickName-based connection matching**: Connections use parameter `NickName` (fallback to `Name`) for robust wiring
  - Serializer emits `NickName` in connection endpoints
  - `ConnectionManager` and `DependencyGraphUtils` resolve by ParamIndex → NickName → Name
- **Group serialization**: Serializer extracts group information with member ID mapping

### Changed

#### Breaking Changes

- **Deserialization flow**: `GhJsonDeserializer.Deserialize()` no longer creates connections or sets component pivots
  - Use `ComponentPlacer` for positioning
  - Use `ConnectionManager.CreateConnections()` for wiring
  - Use `GroupManager.CreateGroups()` for group recreation
- **Connection endpoint matching**: Changed from `Name`-only to `ParamIndex` → `NickName` → `Name` priority
- **Persistent data handling**: Removed `ParameterSettings.PersistentData` extraction (handled via `SchemaProperties`)

#### Improvements

- **Script component state**: Avoid re-applying script code in `ApplyComponentState()` to prevent parameter regeneration
- **C# type hint injection**: Minimal injection replaces `object` parameter types with base types from `TypeHint`
- **Geometric serializer initialization**: Ensured in both serializer and deserializer for consistent behavior

### Fixed

- **Script parameter type hints**: Type hints now correctly applied via reflection using `TypeHints.Select()`
- **VB script parameter handling**: Proper `Param_ScriptVariable` instantiation for VB.NET components
- **Parameter access modes**: Correct mapping between `GH_ParamAccess` and string representations

### Removed

- **Legacy connection creation**: Removed in-deserializer connection helper (replaced by `Canvas.ConnectionManager`)
- **Direct pivot setting**: Removed from deserializer (delegated to placement utilities)

## [1.0.0] - Initial Release

### Initial Features

- Core GhJSON serialization and deserialization
- Component and parameter property handling
- Connection and group support
- Schema property system
- Data type serializers for geometric types
- Validation and fixing utilities
- Dependency graph layout utilities
