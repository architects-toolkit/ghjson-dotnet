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

                // Fallback to fuzzy name resolution (alias + approximate matching)
                if (obj == null)
                {
#if DEBUG
                    Debug.WriteLine($"[ComponentInstantiator.Create] Exact name match failed, trying fuzzy resolution: {component.Name}");
#endif
                    obj = CreateByFuzzyName(component.Name, component.Library);
                }
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

        private static IGH_DocumentObject? CreateByGuid(Guid componentGuid)
        {
            try
            {
                var proxy = Instances.ComponentServer.EmitObjectProxy(componentGuid);
#if DEBUG
                if (proxy != null)
                {
                    Debug.WriteLine($"[CreateByGuid] Found proxy for GUID {componentGuid}: {proxy.Desc.Name} (Lib: {proxy.Desc.Category}, Type: {proxy.Type?.Name})");
                }
                else
                {
                    Debug.WriteLine($"[CreateByGuid] No proxy found for GUID: {componentGuid}");
                }
#endif
                return proxy?.CreateInstance();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[CreateByGuid] Exception creating component by GUID {componentGuid}: {ex.Message}");
#endif
                return null;
            }
        }

        private static IGH_DocumentObject? CreateByName(string name, string? library)
        {
            try
            {
                var matches = new List<IGH_ObjectProxy>();
                
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

                        matches.Add(proxy);
                    }
                }

#if DEBUG
                if (matches.Count > 0)
                {
                    // Calculate scores for each match
                    var scoredMatches = matches.Select(m => new
                    {
                        Proxy = m,
                        Score = ComponentTypeResolver.CalculateTypePriorityScore(m.Type?.Name),
                        TypeName = m.Type?.Name ?? "unknown"
                    }).ToList();

                    // Sort by score descending
                    scoredMatches.Sort((a, b) => b.Score.CompareTo(a.Score));

                    Debug.WriteLine($"[CreateByName] Found {matches.Count} match(es) for '{name}'{(library != null ? $" in library '{library}'" : "")}:");
                    foreach (var sm in scoredMatches)
                    {
                        var asmVersion = GetAssemblyVersion(sm.Proxy);
                        Debug.WriteLine($"  - {sm.Proxy.Desc.Name} (Lib: {sm.Proxy.Desc.Category}, Ver: {asmVersion}, Type: {sm.TypeName}, Score: {sm.Score})");
                    }
                    
                    // Select highest scored match
                    var selected = scoredMatches[0].Proxy;
                    Debug.WriteLine($"[CreateByName] Selected (highest score {scoredMatches[0].Score}): {selected.Desc.Name} from {selected.Desc.Category} (Type: {scoredMatches[0].TypeName})");
                }
                else
                {
                    Debug.WriteLine($"[CreateByName] No exact matches found for '{name}'");
                }
#endif

                // Return highest scored match (or first if all scores equal)
                var finalMatches = matches.Select(m => new
                {
                    Proxy = m,
                    Score = ComponentTypeResolver.CalculateTypePriorityScore(m.Type?.Name)
                }).OrderByDescending(x => x.Score).ToList();
                
                return finalMatches.FirstOrDefault()?.Proxy.CreateInstance();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[CreateByName] Exception creating component by name '{name}': {ex.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Gets the assembly version from a component proxy for debugging purposes.
        /// </summary>
        private static string GetAssemblyVersion(IGH_ObjectProxy proxy)
        {
            try
            {
                var version = proxy.Type?.Assembly?.GetName()?.Version;
                return version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Checks if a component can be instantiated.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>True if the component can be created.</returns>
        public static bool CanInstantiate(GhJsonComponent component)
        {
#if DEBUG
            Debug.WriteLine($"[CanInstantiate] Checking if component can be instantiated: {component.Name ?? "unknown"}");
#endif
            
            if (component.ComponentGuid.HasValue && component.ComponentGuid != Guid.Empty)
            {
                var proxy = Instances.ComponentServer.EmitObjectProxy(component.ComponentGuid.Value);
                if (proxy != null)
                {
#if DEBUG
                    Debug.WriteLine($"[CanInstantiate] Can instantiate by GUID: {component.ComponentGuid}");
#endif
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(component.Name))
            {
                foreach (var proxy in Instances.ComponentServer.ObjectProxies)
                {
                    if (proxy.Desc.Name.Equals(component.Name, StringComparison.OrdinalIgnoreCase))
                    {
#if DEBUG
                        Debug.WriteLine($"[CanInstantiate] Can instantiate by exact name match: {component.Name}");
#endif
                        return true;
                    }
                }

                // Try fuzzy resolution as fallback
                var resolvedName = ResolveComponentName(component.Name, component.Library);
                if (resolvedName != null)
                {
#if DEBUG
                    Debug.WriteLine($"[CanInstantiate] Can instantiate by fuzzy name resolution: {component.Name} → {resolvedName}");
#endif
                    return true;
                }
            }

#if DEBUG
            Debug.WriteLine($"[CanInstantiate] Cannot instantiate component: {component.Name ?? component.ComponentGuid?.ToString() ?? "unknown"}");
#endif
            return false;
        }

        /// <summary>
        /// Attempts to create a component using fuzzy name resolution.
        /// First checks the alias dictionary, then fuzzy-matches against all registered components.
        /// </summary>
        private static IGH_DocumentObject? CreateByFuzzyName(string name, string? library)
        {
            var resolvedName = ResolveComponentName(name, library);
            if (resolvedName == null)
            {
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[ComponentInstantiator.CreateByFuzzyName] Resolved '{name}' → '{resolvedName}'");
#endif

            return CreateByName(resolvedName, library);
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

#if DEBUG
            Debug.WriteLine($"[ResolveComponentName] Resolving '{name}' against {knownNames.Count()} known component names");
#endif

            var result = ComponentNameResolver.Resolve(name, knownNames);
            
#if DEBUG
            if (result != null)
            {
                Debug.WriteLine($"[ResolveComponentName] Resolved '{name}' → '{result}'");
            }
            else
            {
                Debug.WriteLine($"[ResolveComponentName] Could not resolve '{name}'");
            }
#endif

            return result;
        }
    }
}
