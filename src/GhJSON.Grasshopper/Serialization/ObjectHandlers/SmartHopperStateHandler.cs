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
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler that captures the selected AI provider name from SmartHopper
    /// components. Uses reflection to avoid a hard dependency on SmartHopper assemblies.
    /// Only serializes <c>selectedProviderName</c>; all other SmartHopper properties are
    /// design-time defaults and do not need to be persisted.
    /// </summary>
    internal sealed class SmartHopperStateHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => "https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/extensions/smarthopper.state.schema.json";

        /// <inheritdoc/>
        public string ExtensionKey => "smarthopper.state";

        /// <inheritdoc/>
        public Guid ComponentGuid => Guid.Empty;

        /// <inheritdoc/>
        public string ComponentName => "SmartHopper";

        // Cached reflection members
        private static readonly Type? ProviderComponentType;
        private static readonly PropertyInfo? SelectedProviderNameProperty;
        private static readonly MethodInfo? SetSelectedProviderNameMethod;

        static SmartHopperStateHandler()
        {
            // Try to locate the SmartHopper IProviderComponent interface via reflection.
            // This allows the handler to work when SmartHopper is loaded, without creating
            // a compile-time dependency.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType("SmartHopper.Core.ComponentBase.Contracts.IProviderComponent");
                    if (type != null && type.IsInterface)
                    {
                        ProviderComponentType = type;
                        SelectedProviderNameProperty = type.GetProperty("SelectedProviderName", BindingFlags.Public | BindingFlags.Instance);
                        SetSelectedProviderNameMethod = type.GetMethod("SetSelectedProviderName", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                        break;
                    }
                }
                catch
                {
                    // Ignore assembly reflection errors
                }
            }
        }

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            if (ProviderComponentType == null || SelectedProviderNameProperty == null)
            {
                return false;
            }

            return ProviderComponentType.IsAssignableFrom(obj.GetType());
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (SelectedProviderNameProperty == null)
            {
                return;
            }

            var providerName = SelectedProviderNameProperty.GetValue(obj) as string;
            if (string.IsNullOrEmpty(providerName) || providerName == "Default")
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();
            component.ComponentState.Extensions[this.ExtensionKey] = new Dictionary<string, object>
            {
                ["selectedProviderName"] = providerName,
            };
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (SetSelectedProviderNameMethod == null ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(this.ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> data)
            {
                return;
            }

            if (data.TryGetValue("selectedProviderName", out var nameValue) && nameValue != null)
            {
                var name = nameValue.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    SetSelectedProviderNameMethod.Invoke(obj, new object?[] { name });
                }
            }
        }
    }
}
