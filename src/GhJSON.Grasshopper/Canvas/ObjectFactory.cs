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
using Grasshopper;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Factory for creating Grasshopper component instances from proxies.
    /// Provides methods to find component proxies by GUID or name and create instances.
    /// </summary>
    public static class ObjectFactory
    {
        /// <summary>
        /// Finds a component proxy by its GUID.
        /// </summary>
        /// <param name="guid">The component GUID to find.</param>
        /// <returns>The object proxy, or null if not found.</returns>
        public static IGH_ObjectProxy? FindProxy(Guid guid)
        {
            var objectProxy = Instances.ComponentServer.EmitObjectProxy(guid);

            if (objectProxy != null)
            {
                return objectProxy;
            }

            Debug.WriteLine($"[ObjectFactory] Object not found by guid: {guid}");
            return null;
        }

        /// <summary>
        /// Finds a component proxy by its name.
        /// </summary>
        /// <param name="name">The component name to find.</param>
        /// <returns>The object proxy, or null if not found.</returns>
        public static IGH_ObjectProxy? FindProxy(string name)
        {
            var objectProxy = Instances.ComponentServer.FindObjectByName(name, true, true);

            if (objectProxy != null)
            {
                return objectProxy;
            }

            Debug.WriteLine($"[ObjectFactory] Object not found by name: {name}");
            return null;
        }

        /// <summary>
        /// Finds a component proxy by GUID first, then by name as fallback.
        /// </summary>
        /// <param name="guid">The component GUID to try first.</param>
        /// <param name="name">The component name as fallback.</param>
        /// <returns>The object proxy, or null if not found.</returns>
        public static IGH_ObjectProxy? FindProxy(Guid guid, string? name)
        {
            if (guid == Guid.Empty && !string.IsNullOrEmpty(name))
            {
                return FindProxy(name);
            }
            else if (guid != Guid.Empty)
            {
                return FindProxy(guid);
            }

            return null;
        }

        /// <summary>
        /// Creates a document object instance from a proxy and initializes its attributes.
        /// </summary>
        /// <param name="objectProxy">The proxy to create an instance from.</param>
        /// <returns>The created document object with initialized attributes.</returns>
        public static IGH_DocumentObject CreateInstance(IGH_ObjectProxy objectProxy)
        {
            var documentObject = objectProxy.CreateInstance();
            documentObject.CreateAttributes();
            return documentObject;
        }

        /// <summary>
        /// Checks if a component GUID exists in the Grasshopper system.
        /// </summary>
        /// <param name="componentGuid">The component GUID to validate.</param>
        /// <returns>True if the component exists; otherwise false.</returns>
        public static bool IsValidComponent(Guid componentGuid)
        {
            try
            {
                var proxy = FindProxy(componentGuid);
                return proxy != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
