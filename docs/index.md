# GhJSON.NET Documentation

This folder contains documentation for the **ghjson-dotnet** project — the .NET implementation of the [GhJSON specification](https://github.com/architects-toolkit/ghjson-spec).

## Guides

- [Usage Guide](./Usage/index.md) — Quick start, code examples, and common patterns
  - [Metadata](./Usage/metadata.md) — Document metadata configuration and auto-population
  - [Name Resolution](./Usage/name-resolution.md) — Fuzzy matching for component and parameter names
- [Architecture](./ARCHITECTURE.md) — Project structure, API surface, extensibility, and design decisions
  - [Data Type Serialization](./ARCHITECTURE.md#data-type-serialization) — Prefix-based format, built-in serializers, extensibility
  - [Parameter Settings](./ARCHITECTURE.md#parameter-settings) — Input/output parameter configuration
  - [Component State and Extensions](./ARCHITECTURE.md#component-state-and-extensions) — Extension mechanism for component-specific properties
  - [Object Serialization Process](./ARCHITECTURE.md#object-serialization-and-deserialization-process) — Handler orchestration and priority system
- [NuGet Publishing](./NUGET-PUBLISHING.md) — Release workflow and NuGet package publishing
- [Release Workflow](../.github/workflows/RELEASE_WORKFLOW.md) — CI/CD workflows, PR validations, and release process

## Quick Reference

See the [README](../README.md) for:

- **Supported Features** — Core operations and Grasshopper integration capabilities
- **Supported Data Types** — 15 built-in serializers (text, number, integer, boolean, color, point, vector, line, plane, circle, arc, box, rectangle, interval, bounds)
- **Supported Component Handlers** — 12 extension handlers (number slider, panel, scribble, value list, button, toggle, colour swatch, C#/Python/IronPython/GhPython/VB scripts) + 9 core handlers

## Packages

- **GhJSON.Core** — Platform-independent document model and operations (read, write, validate, fix, merge, migrate). No Grasshopper or Rhino dependencies.
- **GhJSON.Grasshopper** — Grasshopper integration (serialize from canvas, place on canvas, query, data type serializers, object handlers). Requires Rhino 8+.

## External References

- [GhJSON Specification](https://github.com/architects-toolkit/ghjson-spec) — Official format specification
- [JSON Schema v1.0](https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json) — Machine-readable validation schema
- [SmartHopper](https://github.com/architects-toolkit/SmartHopper) — AI-powered Grasshopper plugin (uses GhJSON)
