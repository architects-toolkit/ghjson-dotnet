# GhJSON.NET Documentation

This folder contains documentation for the **ghjson-dotnet** project — the .NET implementation of the [GhJSON specification](https://github.com/architects-toolkit/ghjson-spec).

## Guides

- [Usage Guide](./Usage/index.md) — Quick start, code examples, and common patterns
  - [Metadata](./Usage/metadata.md) — Document metadata configuration and auto-population
- [Architecture](./ARCHITECTURE.md) — Project structure, API surface, extensibility, and design decisions
- [NuGet Publishing](./NUGET-PUBLISHING.md) — Release workflow and NuGet package publishing

## Packages

- **GhJSON.Core** — Platform-independent document model and operations (read, write, validate, fix, merge, migrate). No Grasshopper or Rhino dependencies.
- **GhJSON.Grasshopper** — Grasshopper integration (serialize from canvas, place on canvas, query, data type serializers, object handlers). Requires Rhino 8+.

## External References

- [GhJSON Specification](https://github.com/architects-toolkit/ghjson-spec) — Official format specification
- [JSON Schema v1.0](https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ghjson.schema.json) — Machine-readable validation schema
- [SmartHopper](https://github.com/architects-toolkit/SmartHopper) — AI-powered Grasshopper plugin (uses GhJSON)
