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

namespace GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyHandlers
{
    /// <summary>
    /// Registry for property handlers that manages handler discovery and selection.
    /// </summary>
    public class PropertyHandlerRegistry
    {
        private readonly List<IPropertyHandler> _handlers;
        private static readonly Lazy<PropertyHandlerRegistry> _instance = new(() => new PropertyHandlerRegistry());

        /// <summary>
        /// Gets the singleton instance of the property handler registry.
        /// </summary>
        public static PropertyHandlerRegistry Instance => _instance.Value;

        private PropertyHandlerRegistry()
        {
            _handlers = new List<IPropertyHandler>();
            RegisterDefaultHandlers();
        }

        public void RegisterHandler(IPropertyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.Add(handler);
            _handlers.Sort((h1, h2) => h2.Priority.CompareTo(h1.Priority));
        }

        public IPropertyHandler? GetHandler(object sourceObject, string propertyName)
        {
            if (sourceObject == null || string.IsNullOrEmpty(propertyName))
                return null;

            return _handlers.FirstOrDefault(handler => handler.CanHandle(sourceObject, propertyName));
        }

        public object? ExtractProperty(object sourceObject, string propertyName)
        {
            var handler = GetHandler(sourceObject, propertyName);
            return handler?.ExtractProperty(sourceObject, propertyName);
        }

        public bool ApplyProperty(object targetObject, string propertyName, object? value)
        {
            try
            {
                var handler = GetHandler(targetObject, propertyName);
                return handler?.ApplyProperty(targetObject, propertyName, value) ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyHandlerRegistry] Failed to apply property '{propertyName}' to {targetObject?.GetType().Name ?? "null"}: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<string> GetRelatedProperties(object sourceObject, string propertyName)
        {
            var handler = GetHandler(sourceObject, propertyName);
            return handler?.GetRelatedProperties(sourceObject, propertyName) ?? Enumerable.Empty<string>();
        }

        public Dictionary<string, object> ExtractProperties(object sourceObject, IEnumerable<string> propertyNames)
        {
            var result = new Dictionary<string, object>();
            var processedProperties = new HashSet<string>();

            foreach (var propertyName in propertyNames)
            {
                if (processedProperties.Contains(propertyName))
                    continue;

                var value = ExtractProperty(sourceObject, propertyName);
                if (value != null)
                {
                    result[propertyName] = value;
                }

                processedProperties.Add(propertyName);

                var relatedProperties = GetRelatedProperties(sourceObject, propertyName);
                foreach (var relatedProperty in relatedProperties)
                {
                    if (!processedProperties.Contains(relatedProperty))
                    {
                        var relatedValue = ExtractProperty(sourceObject, relatedProperty);
                        if (relatedValue != null)
                        {
                            result[relatedProperty] = relatedValue;
                        }

                        processedProperties.Add(relatedProperty);
                    }
                }
            }

            return result;
        }

        public Dictionary<string, bool> ApplyProperties(object targetObject, Dictionary<string, object> properties)
        {
            var results = new Dictionary<string, bool>();

            foreach (var kvp in properties)
            {
                results[kvp.Key] = ApplyProperty(targetObject, kvp.Key, kvp.Value);
            }

            return results;
        }

        private void RegisterDefaultHandlers()
        {
            RegisterHandler(new SpecializedPropertyHandlers.PersistentDataPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.PanelPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.ValueListItemsPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.ValueListModePropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.SliderCurrentValuePropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.SliderRoundingPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.ExpressionPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.ColorPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.FontPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.DataMappingPropertyHandler());
            RegisterHandler(new SpecializedPropertyHandlers.DefaultPropertyHandler());
        }
    }
}
