# Name Resolution

GhJSON includes fuzzy name resolution for Grasshopper component and parameter names. This is useful when working with AI-generated GhJSON where the AI may use informal names, abbreviations, or slight misspellings.

## How It Works

Name resolution uses a multi-strategy approach, applied in order:

1. **Alias lookup** — A built-in dictionary maps common shorthand names to canonical Grasshopper names (e.g., `"slider"` → `"Number Slider"`, `"pt"` → `"Point"`)
2. **Exact match** — Case-insensitive exact string comparison
3. **Normalized match** — Strips spaces, underscores, and hyphens before comparing (e.g., `"number_slider"` matches `"Number Slider"`)
4. **Prefix match** — Returns a candidate if the input is a unique prefix
5. **Contains match** — Returns the shortest candidate containing the input
6. **Levenshtein distance** — Tolerates small typos (configurable max distance)

## Component Name Resolution

```csharp
using GhJSON.Core;

// Alias-only resolution (no fuzzy matching against a candidate list)
string? name = GhJson.ResolveComponentAlias("slider");
// Returns: "Number Slider"

// Full resolution with fuzzy matching against known names
var knownNames = new[] { "Addition", "Subtraction", "Multiplication", "Division" };
string? resolved = GhJson.ResolveComponentName("Addtion", knownNames);
// Returns: "Addition" (Levenshtein distance 1)
```

### Built-in Component Aliases

| Alias | Canonical Name |
|---|---|
| `slider`, `numberslider`, `numslider` | Number Slider |
| `panel` | Panel |
| `pt`, `point` | Point |
| `crv`, `curve` | Curve |
| `bool`, `boolean` | Boolean |
| `toggle`, `booleantoggle` | Boolean Toggle |
| `add`, `plus`, `addition` | Addition |
| `sub`, `minus`, `subtraction` | Subtraction |
| `mul`, `multiply`, `multiplication` | Multiplication |
| `div`, `divide`, `division` | Division |
| `cube`, `box` | Box |
| `rect`, `rectangle` | Rectangle |
| `circ`, `circle` | Circle |
| `cyl`, `cylinder` | Cylinder |
| `xyz`, `constructpoint`, `ptxyz` | Construct Point |
| `vec`, `vector`, `vectorxyz` | Vector XYZ |
| `loft` | Loft |
| `series` | Series |
| `range` | Range |
| `move` | Move |
| `rotate` | Rotate |
| `scale` | Scale |

See `ComponentNameResolver.cs` for the full list.

## Parameter Name Resolution

```csharp
using GhJSON.Core;

// Alias-only resolution
string? param = GhJson.ResolveParameterAlias("r");
// Returns: "Radius"

// Full resolution with fuzzy matching
var knownParams = new[] { "Radius", "Plane", "Point" };
string? resolved = GhJson.ResolveParameterName("Radus", knownParams);
// Returns: "Radius" (Levenshtein distance 1)
```

### Built-in Parameter Aliases

| Alias | Canonical Name |
|---|---|
| `r`, `radius` | Radius |
| `pt`, `point` | Point |
| `num`, `number` | Number |
| `geo`, `geometry` | Geometry |
| `crv`, `curve` | Curve |
| `srf`, `surface` | Surface |
| `dir`, `direction` | Direction |
| `vec`, `vector` | Vector |
| `a` | A |
| `b` | B |
| `result` | Result |
| `x`, `y`, `z` | X, Y, Z |
| `width` | X Size |
| `length` | Y Size |
| `height` | Z Size |
| `i`, `index` | Index |

See `ParameterNameResolver.cs` for the full list.

## Automatic Integration

Name resolution is automatically applied as a fallback during deserialization:

- **Component instantiation** (`ComponentInstantiator`): When a component name doesn't match any registered Grasshopper component exactly, fuzzy resolution is attempted before failing.
- **Connection wiring** (`CanvasPlacer`): When a parameter name in a connection endpoint doesn't match any parameter on the target component exactly, fuzzy resolution is attempted.

This means GhJSON documents with informal names like `"slider"` or slight typos like `"Addtion"` will still deserialize correctly when placed on the canvas via `GhJsonGrasshopper.Put()`.
