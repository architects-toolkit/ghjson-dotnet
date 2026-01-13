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

        public static readonly Dictionary<SerializationContext, PropertyFilterRule> ContextRules = new()
        {
            [SerializationContext.Standard] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = true,
            },
            [SerializationContext.Optimized] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = true,
                AdditionalExcludes = new() { "PersistentData" },
            },
            [SerializationContext.Lite] = new()
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = false,
                AdditionalExcludes = new()
                {
                    "ComponentGuid", "InstanceGuid", "Selected", "DisplayName",
                    "Alignment", "Font", "SpecialCodes", "DrawIndices", "DrawPaths",
                    "PersistentData",
                },
            },
        };
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
        public HashSet<string> AdditionalIncludes { get; set; } = new();
        public HashSet<string> AdditionalExcludes { get; set; } = new();
    }
}
