/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler that captures generic state from Grasshopper components that do not have
    /// a specialized handler. This ensures unsupported components still serialize
    /// enough information to reconstruct their position, I/O, and any recoverable state.
    /// Runs at low priority so specialized handlers run first.
    /// </summary>
    internal sealed class GenericStateHandler : IObjectHandler
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Runs last (lowest priority) so the skip-when-specialized check can see
        /// extensions already written by higher-priority handlers.
        /// </remarks>
        public int Priority => 0;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public string ExtensionKey => "gh.generic.state";

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return true; // Handles all objects, but only acts when no specialized handler captured state
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return component.ComponentState?.Extensions?.ContainsKey(this.ExtensionKey) == true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
#if DEBUG
            Debug.WriteLine($"[GenericStateHandler.Serialize] Checking generic state for: {obj?.Name}, Type: {obj?.GetType().Name}");
#endif

            // Only capture generic state if no specialized extension handler already ran.
            // We detect this by checking if there are any extension keys registered by handlers.
            var existingHandlerKeys = ObjectHandlerRegistry.GetAll()
                .Where(h => h != this && !string.IsNullOrEmpty(h.ExtensionKey))
                .Select(h => h.ExtensionKey!)
                .ToHashSet();

            bool hasSpecializedExtension = component.ComponentState?.Extensions?.Keys
                .Any(k => existingHandlerKeys.Contains(k)) ?? false;

            if (hasSpecializedExtension)
            {
#if DEBUG
                Debug.WriteLine($"[GenericStateHandler.Serialize] SKIPPED {obj?.Name}: specialized handler already captured state");
#endif
                return;
            }

            // Capture generic properties via reflection
            var capturedProperties = new Dictionary<string, object?>();
            var type = obj.GetType();

            // Capture interesting public instance properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propName = prop.Name;
                // Skip common Grasshopper base properties already handled by other handlers
                if (IsBaseProperty(propName))
                {
                    continue;
                }

                try
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        capturedProperties[propName] = SerializeValue(value);
                    }
                }
                catch
                {
                    // Ignore properties that throw on read
                }
            }

            // Capture interesting public fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsBaseProperty(field.Name))
                {
                    continue;
                }

                try
                {
                    var value = field.GetValue(obj);
                    if (value != null)
                    {
                        capturedProperties[field.Name] = SerializeValue(value);
                    }
                }
                catch
                {
                    // Ignore fields that throw on read
                }
            }

            if (capturedProperties.Count == 0)
            {
#if DEBUG
                Debug.WriteLine($"[GenericStateHandler.Serialize] No generic properties captured for: {obj?.Name}");
#endif
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();
            component.ComponentState.Extensions[this.ExtensionKey] = new JObject
            {
                ["fullTypeName"] = type.FullName,
                ["assemblyName"] = type.Assembly.GetName().Name,
                ["properties"] = JObject.FromObject(capturedProperties),
            };

#if DEBUG
            Debug.WriteLine($"[GenericStateHandler.Serialize] Captured {capturedProperties.Count} generic properties for: {obj?.Name}");
#endif
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
#if DEBUG
            Debug.WriteLine($"[GenericStateHandler.Deserialize] Skipping deserialization for: {obj?.Name} — state is for future handler use");
#endif
        }

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly HashSet<string> BasePropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            // Identification & structural (already handled by other handlers)
            "Name",
            "NickName",
            "ComponentGuid",
            "InstanceGuid",
            "Category",
            "SubCategory",
            "Description",
            "Locked",
            "Hidden",
            "Attributes",
            "Params",
            "DataType",
            "Access",
            "Optional",
            "SourceCount",
            "Recipients",
            "VolatileData",
            "PersistentData",

            // Runtime / execution state (irrelevant for persistence)
            "ProcessorTime",
            "CurrentState",
            "Phase",
            "RuntimeMessageLevel",
            "InstanceDescription",
            "Message",
            "RunCount",
            "InConstructor",
            "InPreSolve",
            "VolatileDataCount",
            "Run",
            "RunOnlyOnInputChanges",
            "IsDataProvider",
            "DataComparison",
            "PrincipalParameterIndex",
            "IsValidPrincipalParameterIndex",

            // UI / icon metadata (reconstructible, not stateful)
            "Icon_24x24",
            "Icon_24x24_Locked",
            "IconDisplayMode",
            "IconCapableUI",

            // Structural / metadata (reconstructible from component type)
            "HasCategory",
            "HasSubCategory",
            "MutableNickName",
            "Obsolete",
            "IsPreviewCapable",
            "IsBakeCapable",
            "IsPrincipal",
            "TypeName",
            "Type",
            "Exposure",
            "Keywords",

            // Internal wiring / param metadata (reconstructible)
            "Sources",
            "HasProxySources",
            "ProxySourceCount",
            "StateTags",
            "WireDisplay",
            "DataMapping",
            "Reverse",
            "Simplify",

            // Complex geometry containers (runtime-only, not state)
            "ClippingBox",
        };

        /// <summary>
        /// Checks whether a property name is a base Grasshopper property already
        /// handled by other handlers, or is runtime/computed/internal noise that
        /// should never be persisted.
        /// </summary>
        private static bool IsBaseProperty(string name)
        {
            return BasePropertyNames.Contains(name);
        }

        /// <summary>
        /// Serializes a value to a JSON-compatible representation.
        /// Primitives, strings, and enums are returned directly.
        /// Complex objects are converted to a JObject with type info.
        /// </summary>
        private static object? SerializeValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();

            // Direct JSON-serializable types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                return value;
            }

            if (type.IsEnum)
            {
                return value.ToString();
            }

            // Nullable types
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return value;
            }

            // GUID
            if (value is Guid guid)
            {
                return guid.ToString();
            }

            // DateTime
            if (value is DateTime dt)
            {
                return dt.ToString("O");
            }

            // Collections
            if (value is System.Collections.IEnumerable enumerable && type != typeof(string))
            {
                var items = new JArray();
                foreach (var item in enumerable)
                {
                    items.Add(JToken.FromObject(SerializeValue(item) ?? JValue.CreateNull()));
                }

                return items;
            }

            // Complex objects: try to serialize as JObject, but limit depth
            try
            {
                var jObj = JObject.FromObject(value);
                jObj["__type"] = type.FullName;
                return jObj;
            }
            catch
            {
                // Fallback: store the string representation and type
                return new JObject
                {
                    ["__type"] = type.FullName,
                    ["__string"] = value.ToString(),
                };
            }
        }
    }
}
