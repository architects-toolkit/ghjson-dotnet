# Changelog

All notable changes to the GhJSON project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - Unreleased

### Added

- Learning from Grasshopper MCP (https://github.com/alfredatnycu/grasshopper-mcp): This is an open-source MCP to connect Grasshopper with Claude Desktop using MIT License. This open-source license is compatible with our Apache License 2.0. That's why we are implementing some Grasshopper MCP features in GhJSON-dotnet. We implemented the following features:
  - Fuzzy name resolution for component and parameter names (`GhJSON.Core.NameResolution`)
    - `ComponentNameResolver`: alias dictionary + fuzzy matching for Grasshopper component names
    - `ParameterNameResolver`: alias dictionary + fuzzy matching for parameter names
    - `FuzzyMatcher`: core utility with exact, normalized, prefix, contains, and Levenshtein matching
    - `NameResolver`: unified public facade exposed via `GhJson.ResolveComponentName()` / `GhJson.ResolveParameterName()`
- Fuzzy fallback in `ComponentInstantiator` when exact name lookup fails during deserialization
- Fuzzy fallback in `CanvasPlacer.GetParameter` when exact parameter name lookup fails during connection wiring

### Changed

- Downgrade Rhino and Grasshopper dependencies to 8.0
- TODO: Updated validation logic to use ghjson.schema.json v1.0

## [1.0.0] - 2026-02-08

- Initial release
