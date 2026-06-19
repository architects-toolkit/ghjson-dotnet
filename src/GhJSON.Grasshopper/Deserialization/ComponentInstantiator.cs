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
using GhJSON.Core.NameResolution;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Deserialization
{
    /// <summary>
    /// Creates Grasshopper document objects from GhJSON components.
    /// </summary>
    internal static class ComponentInstantiator
    {
        /// <summary>
        /// Creates a document object from a GhJSON component definition.
        /// <para>Resolution order (first match wins):</para>
        /// <list type="number">
        ///   <item>Explicit <c>ComponentGuid</c>.</item>
        ///   <item>Exact component name match.</item>
        ///   <item>Alias resolution (<see cref="ComponentNameResolver.ResolveAlias"/> — deterministic).</item>
        ///   <item>Extension key → GUID (deterministic, via registered handlers).</item>
        ///   <item>Extension key → handler's canonical name → fuzzy match (first source of truth).</item>
        ///   <item>Original name → fuzzy match (second source of truth).</item>
        /// </list>
        /// </summary>
        /// <param name="component">The component definition.</param>
        /// <param name="options">Deserialization options.</param>
        /// <returns>The created document object, or null if creation failed.</returns>
        public static IGH_DocumentObject? Create(GhJsonComponent component, DeserializationOptions options)
        {
#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Creating component: {component.Name ?? "unknown"}, GUID: {component.ComponentGuid}");
#endif

            var proxy = Resolve(component);

            IGH_DocumentObject? obj;

            if (proxy == null)
            {
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Failed to resolve component: {component.Name ?? component.ComponentGuid?.ToString()}");
#endif
                return null;
            }

            obj = proxy.CreateInstance();
            if (obj == null)
            {
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Proxy resolved but CreateInstance() returned null for: {proxy.Desc.Name}");
#endif
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Created object: {obj.Name}, Type: {obj.GetType().Name}");
#endif

            // Preserve instance GUID when not regenerating
            if (!options.RegenerateInstanceGuids &&
                component.InstanceGuid.HasValue &&
                component.InstanceGuid.Value != Guid.Empty)
            {
                obj.NewInstanceGuid(component.InstanceGuid.Value);
#if DEBUG
                Debug.WriteLine($"[ComponentInstantiator.Create] Preserved InstanceGuid: {component.InstanceGuid.Value}");
#endif
            }

            // Create default attributes
            obj.CreateAttributes();

            // Apply properties using handlers
            ObjectHandlerOrchestrator.Deserialize(component, obj);

#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.Create] Finished deserializing properties for: {obj.Name}");
#endif

            return obj;
        }

        /// <summary>
        /// Resolves a <see cref="GhJsonComponent"/> to an <see cref="IGH_ObjectProxy"/>
        /// using the 6-step resolution chain. Both <see cref="Create"/> and
        /// <see cref="CanInstantiate"/> delegate to this method to avoid duplication.
        /// </summary>
        private static IGH_ObjectProxy? Resolve(GhJsonComponent component)
        {
            IGH_ObjectProxy? proxy = null;

            // 1. Explicit GUID
            if (component.ComponentGuid.HasValue && component.ComponentGuid != Guid.Empty)
            {
#if DEBUG
                Debug.WriteLine($"[Resolve] Trying GUID: {component.ComponentGuid}");
#endif
                proxy = Instances.ComponentServer.EmitObjectProxy(component.ComponentGuid.Value);
                if (proxy != null) return proxy;
            }

            // 2. Exact name match
            if (!string.IsNullOrEmpty(component.Name))
            {
#if DEBUG
                Debug.WriteLine($"[Resolve] Trying exact name: {component.Name}");
#endif
                proxy = FindProxyByName(component.Name, component.Library);
                if (proxy != null) return proxy;
            }

            // 3. Alias resolution (deterministic, no fuzzy)
            if (!string.IsNullOrEmpty(component.Name))
            {
                var canonical = ComponentNameResolver.ResolveAlias(component.Name);
                if (canonical != null)
                {
#if DEBUG
                    Debug.WriteLine($"[Resolve] Alias resolved '{component.Name}' → '{canonical}'");
#endif
                    proxy = FindProxyByName(canonical, component.Library);
                    if (proxy != null) return proxy;
                }
            }

            // 4. Extension key → GUID (deterministic, via registered handlers)
            var extensions = component.ComponentState?.Extensions;
            if (extensions != null)
            {
                foreach (var key in extensions.Keys)
                {
                    var guid = ObjectHandlerRegistry.ResolveExtensionKeyToGuid(key);
                    if (guid.HasValue)
                    {
#if DEBUG
                        Debug.WriteLine($"[Resolve] Extension key '{key}' → GUID {guid.Value}");
#endif
                        proxy = Instances.ComponentServer.EmitObjectProxy(guid.Value);
                        if (proxy != null) return proxy;
                    }
                }

                // 5. Extension key → handler's canonical name → fuzzy match
                foreach (var key in extensions.Keys)
                {
                    var handlerName = ObjectHandlerRegistry.ResolveExtensionKeyToComponentName(key);
                    if (handlerName != null)
                    {
#if DEBUG
                        Debug.WriteLine($"[Resolve] Extension key '{key}' → handler name '{handlerName}', trying fuzzy");
#endif
                        proxy = FindProxyByFuzzyName(handlerName, null);
                        if (proxy != null) return proxy;
                    }
                }
            }

            // 6. Original name → fuzzy match
            if (!string.IsNullOrEmpty(component.Name))
            {
#if DEBUG
                Debug.WriteLine($"[Resolve] Trying fuzzy name resolution: {component.Name}");
#endif
                proxy = FindProxyByFuzzyName(component.Name, component.Library);
            }

            return proxy;
        }

        /// <summary>
        /// Checks if a component can be instantiated using the same resolution
        /// chain as <see cref="Create"/>.
        /// </summary>
        public static bool CanInstantiate(GhJsonComponent component)
        {
            var proxy = Resolve(component);
#if DEBUG
            Debug.WriteLine(proxy != null
                ? $"[CanInstantiate] Can instantiate: {component.Name ?? "unknown"} → {proxy.Desc.Name}"
                : $"[CanInstantiate] Cannot instantiate: {component.Name ?? component.ComponentGuid?.ToString() ?? "unknown"}");
#endif
            return proxy != null;
        }

        #region Proxy Lookup Helpers

        /// <summary>
        /// Finds the best matching proxy by exact name, optionally filtered by library.
        /// When multiple proxies match, the one with the highest type priority score wins.
        /// </summary>
        private static IGH_ObjectProxy? FindProxyByName(string name, string? library)
        {
            try
            {
                var matches = new List<IGH_ObjectProxy>();

                foreach (var proxy in Instances.ComponentServer.ObjectProxies)
                {
                    if (proxy.Desc.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(library) &&
                            !proxy.Desc.Category.Equals(library, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        matches.Add(proxy);
                    }
                }

                if (matches.Count == 0)
                {
#if DEBUG
                    Debug.WriteLine($"[FindProxyByName] No matches for '{name}'");
#endif
                    return null;
                }

#if DEBUG
                var scored = matches.Select(m => new
                {
                    Proxy = m,
                    Score = ComponentTypeResolver.CalculateTypePriorityScore(m.Type?.Name),
                    TypeName = m.Type?.Name ?? "unknown"
                }).OrderByDescending(x => x.Score).ToList();

                Debug.WriteLine($"[FindProxyByName] Found {matches.Count} match(es) for '{name}':");
                foreach (var s in scored)
                {
                    Debug.WriteLine($"  - {s.Proxy.Desc.Name} (Lib: {s.Proxy.Desc.Category}, Type: {s.TypeName}, Score: {s.Score})");
                }
#endif

                return matches
                    .OrderByDescending(m => ComponentTypeResolver.CalculateTypePriorityScore(m.Type?.Name))
                    .First();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[FindProxyByName] Exception for '{name}': {ex.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Resolves a component name using alias lookup and fuzzy matching, then finds the proxy.
        /// </summary>
        private static IGH_ObjectProxy? FindProxyByFuzzyName(string name, string? library)
        {
            var resolvedName = ResolveComponentName(name, library);
            if (resolvedName == null)
            {
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[FindProxyByFuzzyName] Resolved '{name}' → '{resolvedName}'");
#endif

            return FindProxyByName(resolvedName, library);
        }

        /// <summary>
        /// Resolves a component name using alias lookup and fuzzy matching against
        /// all components registered in the Grasshopper component server.
        /// </summary>
        private static string? ResolveComponentName(string name, string? library)
        {
            var knownNames = Instances.ComponentServer.ObjectProxies
                .Where(p => string.IsNullOrEmpty(library) ||
                            p.Desc.Category.Equals(library, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Desc.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return ComponentNameResolver.Resolve(name, knownNames);
        }

        #endregion
    }
}
