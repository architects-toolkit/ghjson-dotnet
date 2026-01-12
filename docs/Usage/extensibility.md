# Extensibility (data types + component handlers)

## Custom data type serialization

GhJSON uses a prefix-based string representation for many value types (e.g., colors, points, boxes).

In `GhJSON.Core.Serialization.DataTypes`:

- `IDataTypeSerializer`
- `DataTypeRegistry`
- `DataTypeSerializer`

Register a custom serializer at startup:

```csharp
using GhJSON.Core.Serialization.DataTypes;

DataTypeRegistry.Register(new MyCustomSerializer());
```

A serializer implements `IDataTypeSerializer`:

```csharp
public class MyCustomSerializer : IDataTypeSerializer
{
    public string TypeName => "MyType";
    public Type TargetType => typeof(MyType);
    public string Prefix => "mytype";

    public string Serialize(object value) => "mytype:...";
    public object Deserialize(string value) => new MyType();
    public bool Validate(string value) => value != null && value.StartsWith("mytype:");
}
```

## Custom component handlers (Grasshopper)

`GhJSON.Grasshopper` uses a **component handler registry** to isolate component-specific behavior (sliders, panels, scripts, ...).

Register a handler:

```csharp
using GhJSON.Grasshopper;

GhJsonGrasshopper.RegisterHandler(new MyCustomComponentHandler());
```

When you pass a custom `ComponentHandlerRegistry` into `GhJsonGrasshopper.Serialize(...)` or `Deserialize(...)`, that registry will be used for handler selection.
