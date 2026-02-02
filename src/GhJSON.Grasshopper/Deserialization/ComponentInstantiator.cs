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
using System.Diagnostics;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Serialization;
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Deserialization
{
    /// <summary>
    /// Creates Grasshopper document objects from GhJSON components.
    /// </summary>
    internal static class ComponentInstantiator
    {
        /// <summary>
        /// Creates a document object from a GhJSON component definition.
        /// </summary>
        /// <param name="component">The component definition.</param>
        /// <param name="options">Deserialization options.</param>
        /// <returns>The created document object, or null if creation failed.</returns>
        public static IGH_DocumentObject? Create(GhJsonComponent component, DeserializationOptions options)
        {
#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Creating component: {component.Name ?? "unknown"}, GUID: {component.ComponentGuid}");
#endif

            IGH_DocumentObject? obj = null;

            // Try to create by component GUID first (most reliable)
            if (component.ComponentGuid.HasValue && component.ComponentGuid != Guid.Empty)
            {
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Trying to create by GUID: {component.ComponentGuid}");
#endif
                obj = CreateByGuid(component.ComponentGuid.Value);
            }

            // Fallback to name-based creation
            if (obj == null && !string.IsNullOrEmpty(component.Name))
            {
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Trying to create by name: {component.Name}");
#endif
                obj = CreateByName(component.Name, component.Library);
            }

            if (obj == null)
            {
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Failed to create component: {component.Name ?? component.ComponentGuid?.ToString()}");
#endif
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Created object: {obj.Name}, Type: {obj.GetType().Name}");
#endif

            // Create default attributes
            obj.CreateAttributes();

            // Apply properties using handlers
            ObjectHandlerOrchestrator.Deserialize(component, obj);

#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Finished deserializing properties for: {obj.Name}");
#endif

            return obj;
        }

        private static IGH_DocumentObject? CreateByGuid(Guid componentGuid)
        {
            try
            {
                var proxy = Instances.ComponentServer.EmitObjectProxy(componentGuid);
                return proxy?.CreateInstance();
            }
            catch
            {
                return null;
            }
        }

        private static IGH_DocumentObject? CreateByName(string name, string? library)
        {
            try
            {
                // Search for component by name
                foreach (var proxy in Instances.ComponentServer.ObjectProxies)
                {
                    if (proxy.Desc.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        // If library is specified, check it matches
                        if (!string.IsNullOrEmpty(library) &&
                            !proxy.Desc.Category.Equals(library, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        return proxy.CreateInstance();
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a component can be instantiated.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>True if the component can be created.</returns>
        public static bool CanInstantiate(GhJsonComponent component)
        {
            if (component.ComponentGuid.HasValue && component.ComponentGuid != Guid.Empty)
            {
                var proxy = Instances.ComponentServer.EmitObjectProxy(component.ComponentGuid.Value);
                if (proxy != null)
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(component.Name))
            {
                foreach (var proxy in Instances.ComponentServer.ObjectProxies)
                {
                    if (proxy.Desc.Name.Equals(component.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
