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
using System.Linq;
using GhJSON.Grasshopper.Serialization.ScriptComponents;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters
{
    /// <summary>
    /// Provides intelligent property filtering based on object type and serialization context.
    /// </summary>
    public class PropertyFilter
    {
        private readonly SerializationContext _context;
        private readonly PropertyFilterRule _rule;
        private readonly HashSet<string> _allowedProperties;

        public PropertyFilter(SerializationContext context = SerializationContext.Standard)
        {
            _context = context;
            _rule = PropertyFilterConfig.ContextRules[context];
            _allowedProperties = BuildAllowedPropertiesSet();
        }

        public bool ShouldIncludeProperty(string propertyName, object sourceObject)
        {
            if (PropertyFilterConfig.GlobalBlacklist.Contains(propertyName))
            {
                return false;
            }

            if (_rule.AdditionalExcludes.Contains(propertyName))
            {
                return false;
            }

            if (_rule.AdditionalIncludes.Contains(propertyName))
            {
                return true;
            }

            if (_allowedProperties.Contains(propertyName))
            {
                return true;
            }

            var category = GetComponentCategory(sourceObject);
            if (category != ComponentCategory.None && _rule.IncludeCategories.HasFlag(category))
            {
                if (PropertyFilterConfig.CategoryProperties.TryGetValue(category, out var categoryProps))
                {
                    return categoryProps.Contains(propertyName);
                }
            }

            return false;
        }

        public HashSet<string> GetAllowedProperties(object sourceObject)
        {
            var result = new HashSet<string>(_allowedProperties);

            var category = GetComponentCategory(sourceObject);
            if (category != ComponentCategory.None && _rule.IncludeCategories.HasFlag(category))
            {
                if (PropertyFilterConfig.CategoryProperties.TryGetValue(category, out var categoryProps))
                {
                    result.UnionWith(categoryProps);
                }
            }

            result.ExceptWith(PropertyFilterConfig.GlobalBlacklist);
            result.ExceptWith(_rule.AdditionalExcludes);
            result.UnionWith(_rule.AdditionalIncludes);

            return result;
        }

        public static PropertyFilter CreateCustom(PropertyFilterRule customRule)
        {
            var filter = new PropertyFilter(SerializationContext.Standard);
            filter._rule.IncludeCore = customRule.IncludeCore;
            filter._rule.IncludeParameters = customRule.IncludeParameters;
            filter._rule.IncludeComponents = customRule.IncludeComponents;
            filter._rule.IncludeCategories = customRule.IncludeCategories;
            filter._rule.AdditionalIncludes.UnionWith(customRule.AdditionalIncludes);
            filter._rule.AdditionalExcludes.UnionWith(customRule.AdditionalExcludes);

            return filter;
        }

        private HashSet<string> BuildAllowedPropertiesSet()
        {
            var properties = new HashSet<string>();

            if (_rule.IncludeCore)
            {
                properties.UnionWith(PropertyFilterConfig.CoreProperties);
            }

            if (_rule.IncludeParameters)
            {
                properties.UnionWith(PropertyFilterConfig.ParameterProperties);
            }

            if (_rule.IncludeComponents)
            {
                properties.UnionWith(PropertyFilterConfig.ComponentProperties);
            }

            return properties;
        }

        private static ComponentCategory GetComponentCategory(object obj)
        {
            // Script components (prefer GUID-based check)
            if (obj is global::Grasshopper.Kernel.IGH_ActiveObject ao && ScriptComponentFactory.IsScriptComponent(ao))
                return ComponentCategory.Script;

            return obj switch
            {
                GH_Panel => ComponentCategory.Panel,
                GH_Scribble => ComponentCategory.Scribble,
                GH_NumberSlider => ComponentCategory.Slider,
                GH_MultiDimensionalSlider => ComponentCategory.MultidimensionalSlider,
                GH_ValueList => ComponentCategory.ValueList,
                GH_ButtonObject => ComponentCategory.Button,
                GH_BooleanToggle => ComponentCategory.BooleanToggle,
                GH_ColourSwatch => ComponentCategory.ColourSwatch,
                GH_GeometryPipeline => ComponentCategory.GeometryPipeline,
                GH_GraphMapper => ComponentCategory.GraphMapper,
                GH_PathMapper => ComponentCategory.PathMapper,
                GH_ColourWheel => ComponentCategory.ColorWheel,
                GH_DataRecorder => ComponentCategory.DataRecorder,
                _ => ComponentCategory.None
            };
        }
    }

    public static class PropertyFilterExtensions
    {
        public static Dictionary<string, T> FilterProperties<T>(
            this Dictionary<string, T> properties,
            PropertyFilter filter,
            object sourceObject)
        {
            return properties
                .Where(kvp => filter.ShouldIncludeProperty(kvp.Key, sourceObject))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static List<string> GetPropertiesToExtract(this PropertyFilter filter, object sourceObject)
        {
            return filter.GetAllowedProperties(sourceObject).ToList();
        }
    }
}
