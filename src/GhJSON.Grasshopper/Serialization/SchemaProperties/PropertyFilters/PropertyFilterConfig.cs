/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters
{
    /// <summary>
    /// Defines property filtering rules for different object types and contexts.
    /// </summary>
    public static class PropertyFilterConfig
    {
        public static readonly HashSet<string> GlobalBlacklist = new()
        {
            "VolatileData",
            "IsValid",
            "IsValidWhyNot",
            "TypeDescription",
            "TypeName",
            "Boundingbox",
            "ClippingBox",
            "ReferenceID",
            "IsReferencedGeometry",
            "IsGeometryLoaded",
            "QC_Type",
            "BoxName",
            "BoxLeft",
            "BoxRight",
            "Value",
            "IsVisible",
            "humanReadable",
            "Properties",
        };

        public static readonly HashSet<string> CoreProperties = new()
        {
            "NickName",
            "Locked",
            "PersistentData",
        };

        public static readonly HashSet<string> ParameterProperties = new()
        {
            "DataMapping",
            "Simplify",
            "Reverse",
            "Expression",
            "Invert",
            "Unitize",
            "Hidden",
            "Locked",
        };

        public static readonly HashSet<string> ComponentProperties = new()
        {
            "Hidden",
            "DisplayName",
        };

        public static readonly Dictionary<ComponentCategory, HashSet<string>> CategoryProperties = new()
        {
            [ComponentCategory.Panel] = new()
            {
                "Font",
                "Alignment",
                "Multiline",
                "DrawIndices",
                "DrawPaths",
                "SpecialCodes",
            },
            [ComponentCategory.Scribble] = new()
            {
                "Text",
                "Font",
                "Corners",
            },
            [ComponentCategory.Slider] = new()
            {
                "CurrentValue",
                "Minimum",
                "Maximum",
                "Range",
                "Decimals",
                "Rounding",
                "Limit",
                "DisplayFormat",
            },
            [ComponentCategory.MultidimensionalSlider] = new()
            {
                "SliderMode",
                "XInterval",
                "YInterval",
                "ZInterval",
                "X",
                "Y",
                "Z",
            },
            [ComponentCategory.ValueList] = new()
            {
                "ListMode",
                "ListItems",
                "SelectedIndices",
            },
            [ComponentCategory.Button] = new()
            {
                "ExpressionNormal",
                "ExpressionPressed",
            },
            [ComponentCategory.BooleanToggle] = new(),
            [ComponentCategory.ColourSwatch] = new(),
            [ComponentCategory.Script] = new()
            {
                "Script",
                "MarshInputs",
                "MarshOutputs",
                "VariableName",
            },
            [ComponentCategory.GeometryPipeline] = new()
            {
                "LayerFilter",
                "NameFilter",
                "TypeFilter",
                "IncludeLocked",
                "IncludeHidden",
                "GroupByLayer",
                "GroupByType",
            },
            [ComponentCategory.GraphMapper] = new()
            {
                "GraphType",
            },
            [ComponentCategory.PathMapper] = new()
            {
                "Lexers",
            },
            [ComponentCategory.ColorWheel] = new()
            {
                "State",
            },
            [ComponentCategory.DataRecorder] = new()
            {
                "DataLimit",
                "RecordData",
            },
            [ComponentCategory.ItemPicker] = new()
            {
                "TreePath",
                "TreeIndex",
            },
        };

        public static readonly Dictionary<SerializationContext, PropertyFilterRule> ContextRules = new()
        {
            [SerializationContext.Standard] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = true,
                IncludeCategories = ComponentCategory.Essential | ComponentCategory.UI,
            },
            [SerializationContext.Optimized] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = true,
                IncludeCategories = ComponentCategory.Essential | ComponentCategory.UI,
                AdditionalExcludes = new() { "PersistentData" },
            },
            [SerializationContext.Lite] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = false,
                IncludeCategories = ComponentCategory.Essential,
                AdditionalExcludes = new()
                {
                    "ComponentGuid", "InstanceGuid", "Selected", "DisplayName",
                    "Alignment", "Font", "SpecialCodes", "DrawIndices", "DrawPaths",
                    "PersistentData",
                },
            },
        };
    }

    [Flags]
    public enum ComponentCategory
    {
        None = 0,
        Panel = 1 << 0,
        Scribble = 1 << 1,
        Slider = 1 << 2,
        MultidimensionalSlider = 1 << 3,
        ValueList = 1 << 4,
        Button = 1 << 5,
        BooleanToggle = 1 << 6,
        ColourSwatch = 1 << 7,
        Script = 1 << 8,
        GeometryPipeline = 1 << 9,
        GraphMapper = 1 << 10,
        PathMapper = 1 << 11,
        ColorWheel = 1 << 12,
        DataRecorder = 1 << 13,
        ItemPicker = 1 << 14,

        Essential = Panel | Scribble | Slider | ValueList | Script,
        UI = Panel | Scribble | Button | BooleanToggle | ColourSwatch | ColorWheel,
        Data = ValueList | DataRecorder | ItemPicker,
        Advanced = GeometryPipeline | GraphMapper | PathMapper,
        All = ~None,
    }

    public enum SerializationContext
    {
        Standard,
        Optimized,
        Lite,
    }

    public class PropertyFilterRule
    {
        public bool IncludeCore { get; set; } = true;
        public bool IncludeParameters { get; set; } = true;
        public bool IncludeComponents { get; set; } = true;
        public ComponentCategory IncludeCategories { get; set; } = ComponentCategory.All;
        public HashSet<string> AdditionalIncludes { get; set; } = new();
        public HashSet<string> AdditionalExcludes { get; set; } = new();
    }
}
