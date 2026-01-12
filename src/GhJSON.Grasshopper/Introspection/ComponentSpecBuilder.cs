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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Introspection
{
    /// <summary>
    /// Utilities for generating Grasshopper component specifications.
    /// These specs are intended for AI tooling / orchestration scenarios.
    /// </summary>
    public static class ComponentSpecBuilder
    {
        /// <summary>
        /// Generates a component specification from a component name and optional parameters.
        /// </summary>
        /// <param name="componentName">Name or nickname of the component to create.</param>
        /// <param name="parameters">Optional dictionary of parameter name/value pairs.</param>
        /// <param name="position">Optional position for the component. If null, automatic layout will be used.</param>
        /// <param name="instanceGuid">Optional instance GUID. If null, a new GUID will be generated.</param>
        /// <returns>JObject representing the component spec, or null if component not found.</returns>
        public static JObject? GenerateComponentSpec(
            string componentName,
            Dictionary<string, object>? parameters = null,
            PointF? position = null,
            Guid? instanceGuid = null)
        {
            if (string.IsNullOrEmpty(componentName))
            {
                Debug.WriteLine("[ComponentSpecBuilder] Component name is required.");
                return null;
            }

            var proxy = FindProxy(componentName);
            if (proxy == null)
            {
                Debug.WriteLine($"[ComponentSpecBuilder] Component not found: {componentName}");
                return null;
            }

            var instance = CreateInstance(proxy);
            var guid = instanceGuid ?? Guid.NewGuid();

            var ghComponent = new JObject
            {
                ["guid"] = proxy.Guid.ToString(),
                ["name"] = proxy.Desc.Name,
                ["nickname"] = proxy.Desc.NickName,
                ["instanceGuid"] = guid.ToString()
            };

            if (position.HasValue)
            {
                ghComponent["pivot"] = new JObject
                {
                    ["x"] = position.Value.X,
                    ["y"] = position.Value.Y
                };
            }

            if (parameters != null && parameters.Count > 0)
            {
                var paramValues = new JArray();
                foreach (var kvp in parameters)
                {
                    paramValues.Add(new JObject
                    {
                        ["name"] = kvp.Key,
                        ["value"] = JToken.FromObject(kvp.Value)
                    });
                }

                ghComponent["parameterValues"] = paramValues;
            }

            if (instance is IGH_Component comp)
            {
                var inputs = new JArray();
                foreach (var param in comp.Params.Input)
                {
                    inputs.Add(new JObject
                    {
                        ["name"] = param.Name,
                        ["nickname"] = param.NickName,
                        ["description"] = param.Description
                    });
                }

                var outputs = new JArray();
                foreach (var param in comp.Params.Output)
                {
                    outputs.Add(new JObject
                    {
                        ["name"] = param.Name,
                        ["nickname"] = param.NickName,
                        ["description"] = param.Description
                    });
                }

                if (inputs.Count > 0) ghComponent["inputs"] = inputs;
                if (outputs.Count > 0) ghComponent["outputs"] = outputs;
            }

            Debug.WriteLine($"[ComponentSpecBuilder] Generated component spec: {componentName} ({guid})");
            return ghComponent;
        }

        /// <summary>
        /// Generates a complete document container from multiple component specifications.
        /// </summary>
        /// <param name="componentSpecs">List of component specifications.</param>
        /// <returns>Complete document object as JObject.</returns>
        public static JObject? GenerateGhJsonDocument(List<JObject> componentSpecs)
        {
            if (componentSpecs == null || componentSpecs.Count == 0)
            {
                Debug.WriteLine("[ComponentSpecBuilder] No component specifications provided.");
                return null;
            }

            return new JObject
            {
                ["components"] = JArray.FromObject(componentSpecs),
                ["connections"] = new JArray()
            };
        }

        private static IGH_ObjectProxy? FindProxy(string componentName)
        {
            // Find by name (case insensitive), include hidden, include obsolete
            var proxy = Instances.ComponentServer.FindObjectByName(componentName, true, true);
            if (proxy != null)
            {
                return proxy;
            }

            // Fallback to nickname match
            var all = Instances.ComponentServer.ObjectProxies;
            return all.FirstOrDefault(p =>
                string.Equals(p.Desc?.NickName, componentName, StringComparison.OrdinalIgnoreCase));
        }

        private static IGH_DocumentObject? CreateInstance(IGH_ObjectProxy proxy)
        {
            try
            {
                return proxy.CreateInstance();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComponentSpecBuilder] Failed to instantiate proxy: {ex.Message}");
                return null;
            }
        }
    }
}
