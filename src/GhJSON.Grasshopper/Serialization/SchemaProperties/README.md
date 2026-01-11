# Property Management System V2

This document describes the new, maintainable property management system that replaces the old hardcoded PropertyManager approach.

## Overview

The new system provides a clean, flexible way to specify which properties should be included or excluded during Grasshopper object serialization. It separates concerns into distinct, maintainable components:

- **Property Filtering**: Determines which properties to include/exclude
- **Property Handling**: Manages extraction and application of specific property types
- **Property Management**: Orchestrates the entire process

## Key Benefits

### ✅ **Maintainable**
- Clear separation of concerns
- Easy to add new component types
- No more hardcoded property lists scattered throughout code

### ✅ **Flexible**
- Multiple serialization contexts (Standard, Optimized, Lite)
- Fluent builder API for custom configurations
- Component category-based filtering

### ✅ **Extensible**
- Plugin architecture for property handlers
- Easy to add new property types or special handling
- Support for custom filtering rules

### ✅ **Type-Safe**
- Strongly typed configuration
- Compile-time checking of property rules
- Clear interfaces and contracts

## Architecture

```
┌─────────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│   PropertyFilter    │    │  PropertyHandler     │    │  PropertyManager    │
│                     │    │                      │    │                     │
│ • Filtering Rules   │    │ • Extraction Logic   │    │ • Orchestration     │
│ • Context-based     │    │ • Type Conversion    │    │ • High-level API    │
│ • Category Support  │    │ • Special Handling   │    │ • Factory Methods   │
└─────────────────────┘    └──────────────────────┘    └─────────────────────┘
```

## Quick Start

### Basic Usage

```csharp
// Use predefined contexts
var standardManager = PropertyManagerFactory.CreateStandard();
var optimizedManager = PropertyManagerFactory.CreateOptimized();
var liteManager = PropertyManagerFactory.CreateLite();

// Extract properties from any Grasshopper object
var slider = new GH_NumberSlider();
var properties = optimizedManager.ExtractProperties(slider);

// Apply properties to another object
var targetSlider = new GH_NumberSlider();
var results = optimizedManager.ApplyProperties(targetSlider, properties);
```

### Custom Filtering

```csharp
// Build custom filters with fluent API
var customManager = PropertyFilterBuilder
    .Create()
    .WithCore(true)                    // Include core properties
    .WithParameters(true)              // Include parameter properties
    .WithCategories(ComponentCategory.Essential)  // Only essential categories
    .Include("CustomProperty")         // Always include specific properties
    .Exclude("LegacyProperty")        // Always exclude specific properties
    .BuildManager();
```

## Serialization Contexts

The system provides several predefined contexts for common scenarios:

### `SerializationContext.Standard`
- **Purpose**: General-purpose extraction with reasonable fidelity
- **Includes**: Core + Parameters + Components + Essential/UI categories
- **Use Case**: Default serialization

### `SerializationContext.Optimized`
- **Purpose**: Cleaner extraction for downstream processing
- **Includes**: Similar to Standard, but with additional excludes (e.g. may exclude `PersistentData`)
- **Use Case**: When you want a smaller/cleaner payload than Standard

### `SerializationContext.Lite`
- **Purpose**: Minimal extraction
- **Includes**: Core + Parameters (components mostly excluded)
- **Use Case**: Compact payloads

## Component Categories

Properties are organized by component categories for easy management:

```csharp
[Flags]
public enum ComponentCategory
{
    None = 0,
    Panel = 1 << 0,           // GH_Panel properties
    Scribble = 1 << 1,        // GH_Scribble properties
    Slider = 1 << 2,          // GH_NumberSlider properties
    ValueList = 1 << 4,       // GH_ValueList properties
    Script = 1 << 6,          // Script component properties
    // ... more categories

    // Convenience combinations
    Essential = Panel | Scribble | Slider | ValueList | Script,
    UI = Panel | Scribble | Button | ColorWheel,
    All = ~None
}
```

## Property Handlers

The system uses specialized handlers for different property types:

### Built-in Handlers

- **`PersistentDataPropertyHandler`**: Handles parameter data serialization
- **`PanelPropertyHandler`**: Panel-specific properties
- **`ValueListItemsPropertyHandler`** / **`ValueListModePropertyHandler`**: Value list configuration
- **`SliderCurrentValuePropertyHandler`** / **`SliderRoundingPropertyHandler`**: Special formatting for slider values
- **`ExpressionPropertyHandler`**: Reflection-based expression extraction
- **`ColorPropertyHandler`**: Color conversion with DataTypeSerializer
- **`FontPropertyHandler`**: Font property handling
- **`DataMappingPropertyHandler`**: GH_DataMapping enum handling
- **`DefaultPropertyHandler`**: Fallback for standard properties

### Custom Handlers

```csharp
public class CustomPropertyHandler : PropertyHandlerBase
{
    public override int Priority => 50;

    public override bool CanHandle(object sourceObject, string propertyName)
    {
        return propertyName == "MyCustomProperty";
    }

    public override object ExtractProperty(object sourceObject, string propertyName)
    {
        // Custom extraction logic
        return ProcessCustomProperty(sourceObject);
    }

    public override bool ApplyProperty(object targetObject, string propertyName, object value)
    {
        // Custom application logic
        return ApplyCustomProperty(targetObject, value);
    }
}

// Register the custom handler
PropertyHandlerRegistry.Instance.RegisterHandler(new CustomPropertyHandler());
```

## Advanced Scenarios

### Dynamic Filtering

```csharp
public static PropertyManagerV2 CreateDynamicManager(object grasshopperObject)
{
    return grasshopperObject switch
    {
        GH_NumberSlider => PropertyFilterBuilder.Create().BuildManager(),
        GH_Panel => PropertyFilterBuilder.Create().BuildManager(),
        _ => PropertyManagerFactory.CreateOptimized()
    };
}
```

### Conditional Properties

```csharp
var conditionalManager = PropertyFilterBuilder
    .Create()
    .Configure(rule =>
    {
        if (Environment.GetEnvironmentVariable("DEBUG") == "1")
        {
            rule.AdditionalIncludes.Add("DebugInfo");
        }

        if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
        {
            rule.IncludeCategories |= ComponentCategory.Advanced;
        }
    })
    .BuildManager();
```

### Performance Analysis

```csharp
var manager = PropertyManagerFactory.CreateOptimized();
var slider = new GH_NumberSlider();

// Check specific properties
var shouldInclude = manager.ShouldIncludeProperty("CurrentValue", slider);
```

## Migration Guide

### From Old PropertyManager

```csharp
// OLD WAY ❌
var oldManager = new PropertyManager();
var isAllowed = oldManager.IsPropertyInWhitelist("CurrentValue");
var properties = oldManager.ExtractProperties(obj);

// NEW WAY ✅
var newManager = PropertyManagerFactory.CreateOptimized();
var isAllowed = newManager.ShouldIncludeProperty("CurrentValue", obj);
var properties = newManager.ExtractProperties(obj);
```

## Configuration Examples

### Slider-Specific Configuration

```csharp
var sliderManager = PropertyFilterBuilder
    .Create()
    .Include("DisplayFormat", "TickCount")
    .Exclude("Minimum", "Maximum")  // Use CurrentValue format instead
    .BuildManager();
```

### Panel-Specific Configuration

```csharp
var panelManager = PropertyFilterBuilder
    .FromContext(SerializationContext.Optimized)
    .Include("BackgroundColor", "BorderColor")
    .BuildManager();
```

### Debug Configuration

```csharp
var debugManager = PropertyFilterBuilder
    .FromContext(SerializationContext.Optimized)
    .ForDebugging()
    .Include("InstanceDescription", "ComponentGuid", "InstanceGuid")
    .BuildManager();
```

## Best Practices

### ✅ **Do**
- Use predefined contexts when possible
- Create specific managers for different use cases
- Use the builder pattern for complex configurations
- Register custom handlers for special property types
- Inspect extracted results for representative objects to validate your rules

### ❌ **Don't**
- Hardcode property lists in business logic
- Mix filtering logic with extraction logic
- Create overly complex custom rules without testing
- Ignore the component category system
- Forget to handle null values in custom handlers

## Troubleshooting

### Property Not Being Extracted

1. Check if it's in the global blacklist
2. Verify the serialization context includes the property type
3. Check if the component category is included
4. Inspect extracted results to see what's being included/excluded

### Custom Handler Not Working

1. Verify the handler is registered with `PropertyHandlerRegistry`
2. Check the `Priority` value (higher priority = tried first)
3. Ensure `CanHandle()` returns true for your property
4. Debug by setting breakpoints in `GetHandler()` / `CanHandle()`

### Performance Issues

1. Use more restrictive contexts (Lite vs Standard)
2. Limit component categories to only what's needed
3. Profile different configurations with performance examples
4. Consider caching property managers for repeated use

## Future Extensions

The system is designed to be easily extensible:

- **New Component Types**: Add to `ComponentCategory` enum and `PropertyFilterConfig`
- **New Contexts**: Add to `SerializationContext` enum and `ContextRules`
- **New Handlers**: Implement `IPropertyHandler` and register
- **New Filters**: Use `PropertyFilterBuilder` or create custom `PropertyFilterRule`

This architecture ensures the property management system can grow with the project while maintaining clean, maintainable code.
