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
using Grasshopper.Kernel;
using GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties
{
    /// <summary>
    /// Modern, maintainable property management system.
    /// Provides separation of concerns between filtering, extraction, and application
    /// of properties for Grasshopper objects.
    /// </summary>
    public class PropertyManagerV2
    {
        private readonly PropertyFilters.PropertyFilter _filter;
        private readonly PropertyHandlers.PropertyHandlerRegistry _handlerRegistry;

        /// <summary>
        /// Initializes a new instance of <see cref="PropertyManagerV2"/> with the specified context.
        /// </summary>
        /// <param name="context">The serialization context that determines which properties to include.</param>
        public PropertyManagerV2(PropertyFilters.SerializationContext context = PropertyFilters.SerializationContext.Standard)
        {
            _filter = new PropertyFilters.PropertyFilter(context);
            _handlerRegistry = PropertyHandlers.PropertyHandlerRegistry.Instance;
        }

        private PropertyManagerV2(PropertyFilters.PropertyFilter filter)
        {
            _filter = filter;
            _handlerRegistry = PropertyHandlers.PropertyHandlerRegistry.Instance;
        }

        /// <summary>
        /// Creates a PropertyManagerV2 with custom filtering rules.
        /// </summary>
        /// <param name="customRule">Custom property filtering rule.</param>
        /// <returns>New PropertyManagerV2 instance with custom rules.</returns>
        public static PropertyManagerV2 CreateCustom(PropertyFilters.PropertyFilterRule customRule)
        {
            var customFilter = PropertyFilters.PropertyFilter.CreateCustom(customRule);
            return new PropertyManagerV2(customFilter);
        }

        /// <summary>
        /// Determines if a property should be included for the given object.
        /// </summary>
        public bool ShouldIncludeProperty(string propertyName, object sourceObject)
        {
            return _filter.ShouldIncludeProperty(propertyName, sourceObject);
        }

        /// <summary>
        /// Gets all properties that should be extracted from the given object.
        /// </summary>
        public List<string> GetPropertiesToExtract(object sourceObject)
        {
            return _filter.GetAllowedProperties(sourceObject).ToList();
        }

        /// <summary>
        /// Extracts all allowed properties from an object.
        /// </summary>
        /// <param name="sourceObject">The object to extract properties from.</param>
        /// <returns>Dictionary of property names and their values.</returns>
        public Dictionary<string, object> ExtractProperties(object sourceObject)
        {
            var propertiesToExtract = GetPropertiesToExtract(sourceObject);
            var extractedValues = _handlerRegistry.ExtractProperties(sourceObject, propertiesToExtract);

            var result = new Dictionary<string, object>();

            foreach (var kvp in extractedValues)
            {
                if (ShouldIncludeProperty(kvp.Key, sourceObject))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            RemoveIrrelevantProperties(result, sourceObject);

            if (result.ContainsKey("PersistentData") && sourceObject is IGH_Param param)
            {
                bool hasSources = param.Sources != null && param.Sources.Count > 0;
                if (hasSources)
                {
                    result["VolatileData"] = result["PersistentData"];
                    result.Remove("PersistentData");
                }
            }

            return result;
        }

        /// <summary>
        /// Applies properties to a target object.
        /// </summary>
        /// <param name="targetObject">The object to apply properties to.</param>
        /// <param name="properties">Dictionary of property names and values.</param>
        /// <returns>Dictionary indicating success/failure for each property.</returns>
        public Dictionary<string, bool> ApplyProperties(object targetObject, Dictionary<string, object> properties)
        {
            return _handlerRegistry.ApplyProperties(targetObject, properties);
        }

        /// <summary>
        /// Applies a single property to a target object.
        /// </summary>
        public bool ApplyProperty(object targetObject, string propertyName, object? propertyValue)
        {
            return _handlerRegistry.ApplyProperty(targetObject, propertyName, propertyValue);
        }

        /// <summary>
        /// Filters an existing property dictionary based on the current filter rules.
        /// </summary>
        public Dictionary<string, object> FilterProperties(Dictionary<string, object> properties, object sourceObject)
        {
            return properties
                .Where(kvp => _filter.ShouldIncludeProperty(kvp.Key, sourceObject))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private void RemoveIrrelevantProperties(Dictionary<string, object> properties, object sourceObject)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in properties)
            {
                var propertyName = kvp.Key;
                var value = kvp.Value;

                if (IsIrrelevantProperty(propertyName, value, sourceObject))
                {
                    keysToRemove.Add(propertyName);
                }
            }

            foreach (var key in keysToRemove)
            {
                properties.Remove(key);
            }
        }

        private static bool IsIrrelevantProperty(string propertyName, object? value, object sourceObject)
        {
            if (value is bool boolValue && !boolValue)
            {
                return propertyName switch
                {
                    "Locked" => true,
                    "Simplify" => true,
                    "Reverse" => true,
                    "Hidden" => true,
                    "Invert" => true,
                    "Selected" => true,
                    "IsPrincipal" => true,
                    _ => false
                };
            }

            if (value is int intValue && intValue == 0)
            {
                return propertyName switch
                {
                    "Locked" => true,
                    "Simplify" => true,
                    "Reverse" => true,
                    "Hidden" => true,
                    "Invert" => true,
                    "Selected" => true,
                    "IsPrincipal" => true,
                    _ => false
                };
            }

            if (propertyName == "DataMapping")
            {
                int mappingValue = value switch
                {
                    int i => i,
                    Enum e => Convert.ToInt32(e),
                    _ => -1
                };

                if (mappingValue == 0)
                {
                    return true;
                }
            }

            if (propertyName == "NickName" && sourceObject is IGH_ActiveObject ghObj)
            {
                return string.IsNullOrEmpty(value?.ToString()) || value?.ToString() == ghObj.Name;
            }

            // Hidden/Locked are meaningful for parameters (visibility/lock state), but for most components
            // we model these via ComponentState instead of SchemaProperties.
            if ((propertyName == "Locked" || propertyName == "Hidden") && sourceObject is not IGH_Param)
            {
                return true;
            }

            if ((propertyName == "ExpressionNormal" || propertyName == "ExpressionPressed") &&
                sourceObject.GetType().Name == "GH_ButtonObject")
            {
                return true;
            }

            if (propertyName == "PersistentData")
            {
                if (sourceObject is global::Grasshopper.Kernel.Special.GH_BooleanToggle ||
                    sourceObject is global::Grasshopper.Kernel.Special.GH_ColourSwatch)
                {
                    return true;
                }

                if (sourceObject.GetType().Name == "GH_ButtonObject")
                {
                    return true;
                }

                if (sourceObject is global::Grasshopper.Kernel.Special.GH_ValueList)
                {
                    return true;
                }

                if (sourceObject is global::Grasshopper.Kernel.Special.GH_NumberSlider)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Factory class for creating PropertyManagerV2 instances with common configurations.
    /// </summary>
    public static class PropertyManagerFactory
    {
        /// <summary>
        /// Creates a PropertyManagerV2 with standard format.
        /// </summary>
        public static PropertyManagerV2 CreateStandard()
        {
            return new PropertyManagerV2(PropertyFilters.SerializationContext.Standard);
        }

        /// <summary>
        /// Creates a PropertyManagerV2 with optimized format.
        /// </summary>
        public static PropertyManagerV2 CreateOptimized()
        {
            return new PropertyManagerV2(PropertyFilters.SerializationContext.Optimized);
        }

        /// <summary>
        /// Creates a PropertyManagerV2 with lite format.
        /// </summary>
        public static PropertyManagerV2 CreateLite()
        {
            return new PropertyManagerV2(PropertyFilters.SerializationContext.Lite);
        }

        /// <summary>
        /// Creates a PropertyManagerV2 with custom component categories.
        /// </summary>
        public static PropertyManagerV2 CreateWithCategories(PropertyFilters.ComponentCategory includeCategories)
        {
            var customRule = new PropertyFilters.PropertyFilterRule
            {
                IncludeCore = true,
                IncludeParameters = true,
                IncludeComponents = true,
                IncludeCategories = includeCategories
            };

            return PropertyManagerV2.CreateCustom(customRule);
        }
    }
}
