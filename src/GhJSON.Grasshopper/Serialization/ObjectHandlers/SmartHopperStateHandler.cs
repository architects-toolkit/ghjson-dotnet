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
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler that claims all SmartHopper components to prevent
    /// <see cref="GenericStateHandler"/> from capturing irrelevant runtime properties.
    /// For <c>IProviderComponent</c> instances with a non-default provider selection,
    /// serializes <c>selectedProviderName</c>.
    /// For <c>ISelectingComponent</c> instances, stores <c>selectedObjects</c> as
    /// instance GUIDs during handler serialization; these are post-processed to
    /// numeric component IDs by <see cref="GetOperations.CanvasReader"/>.
    /// Uses reflection to avoid a hard dependency on SmartHopper assemblies.
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

        // Cached reflection members for IProviderComponent
        private static readonly Type? ProviderComponentType;
        private static readonly PropertyInfo? SelectedProviderNameProperty;
        private static readonly MethodInfo? SetSelectedProviderNameMethod;

        // Cached reflection members for ISelectingComponent
        private static readonly Type? SelectingComponentType;
        private static readonly PropertyInfo? SelectedObjectsProperty;

        static SmartHopperStateHandler()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var providerType = assembly.GetType("SmartHopper.Core.ComponentBase.Contracts.IProviderComponent");
                    if (providerType != null && providerType.IsInterface)
                    {
                        ProviderComponentType = providerType;
                        SelectedProviderNameProperty = providerType.GetProperty("SelectedProviderName", BindingFlags.Public | BindingFlags.Instance);
                        SetSelectedProviderNameMethod = providerType.GetMethod("SetSelectedProviderName", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    }

                    var selectingType = assembly.GetType("SmartHopper.Core.ComponentBase.Contracts.ISelectingComponent");
                    if (selectingType != null && selectingType.IsInterface)
                    {
                        SelectingComponentType = selectingType;
                        SelectedObjectsProperty = selectingType.GetProperty("SelectedObjects", BindingFlags.Public | BindingFlags.Instance);
                    }

                    if (ProviderComponentType != null && SelectingComponentType != null)
                    {
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
            return obj.GetType().Namespace?.StartsWith("SmartHopper.", StringComparison.Ordinal) == true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var state = new Dictionary<string, object>();

            // Provider name
            var providerName = TryGetProviderName(obj);
            if (!string.IsNullOrEmpty(providerName) && providerName != "Default")
            {
                state["selectedProviderName"] = providerName;
            }

            // Selected objects: store GUIDs here; CanvasReader post-processes to IDs.
            var selectedGuids = TryGetSelectedObjectGuids(obj);
            if (selectedGuids != null && selectedGuids.Count > 0)
            {
                if (ObjectHandlerOrchestrator.CurrentOptions?.IncludeInternalizedData != false)
                {
                    state["selectedObjects"] = selectedGuids;
                }
            }

            // Always write the extension to claim the component, even if empty.
            component.ComponentState.Extensions[this.ExtensionKey] = state;
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

            // selectedObjects is resolved in a late post-placement pass where
            // the full id-to-object mapping is available.
        }

        /// <summary>
        /// Late post-placement resolution for <c>selectedObjects</c> IDs.
        /// Called by <see cref="PutOperations.CanvasPlacer"/> after all components
        /// have been placed and the <paramref name="idToObject"/> mapping is complete.
        /// </summary>
        internal static void ApplySelectedObjects(
            GhJsonComponent component,
            IGH_DocumentObject obj,
            Dictionary<int, IGH_DocumentObject> idToObject)
        {
            if (SelectedObjectsProperty == null ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue("smarthopper.state", out var extData) ||
                extData is not Dictionary<string, object> data ||
                !data.TryGetValue("selectedObjects", out var idsValue))
            {
                return;
            }

            var idList = idsValue as List<object> ?? (idsValue as System.Collections.IEnumerable)?.Cast<object>().ToList();
            if (idList == null || idList.Count == 0)
            {
                return;
            }

            var selectedObjects = SelectedObjectsProperty.GetValue(obj) as System.Collections.IList;
            if (selectedObjects == null)
            {
                return;
            }

            selectedObjects.Clear();

            foreach (var idObj in idList)
            {
                if (idObj is int id && idToObject.TryGetValue(id, out var foundObj))
                {
                    selectedObjects.Add(foundObj);
                }
                else if (idObj is long longId && idToObject.TryGetValue((int)longId, out var foundObj2))
                {
                    selectedObjects.Add(foundObj2);
                }
            }
        }

        /// <summary>
        /// Post-processes <c>selectedObjects</c> GUID strings to numeric IDs
        /// after all component IDs have been assigned.
        /// Called by <see cref="GetOperations.CanvasReader"/>.
        /// </summary>
        internal static void PostProcessSelectedObjectsToIds(
            List<GhJsonComponent> components,
            Dictionary<Guid, int> guidToId)
        {
            foreach (var component in components)
            {
                if (component.ComponentState?.Extensions == null ||
                    !component.ComponentState.Extensions.TryGetValue("smarthopper.state", out var extData) ||
                    extData is not Dictionary<string, object> data ||
                    !data.TryGetValue("selectedObjects", out var guidsValue))
                {
                    continue;
                }

                var guidList = guidsValue as List<string>;
                if (guidList == null || guidList.Count == 0)
                {
                    continue;
                }

                var ids = new List<int>();
                foreach (var guidStr in guidList)
                {
                    if (Guid.TryParse(guidStr, out var guid) && guidToId.TryGetValue(guid, out var id))
                    {
                        ids.Add(id);
                    }
                }

                if (ids.Count > 0)
                {
                    data["selectedObjects"] = ids;
                }
                else
                {
                    data.Remove("selectedObjects");
                }
            }
        }

        private static string? TryGetProviderName(IGH_DocumentObject obj)
        {
            if (ProviderComponentType == null ||
                SelectedProviderNameProperty == null ||
                !ProviderComponentType.IsAssignableFrom(obj.GetType()))
            {
                return null;
            }

            return SelectedProviderNameProperty.GetValue(obj) as string;
        }

        private static List<string>? TryGetSelectedObjectGuids(IGH_DocumentObject obj)
        {
            if (SelectingComponentType == null ||
                SelectedObjectsProperty == null ||
                !SelectingComponentType.IsAssignableFrom(obj.GetType()))
            {
                return null;
            }

            var list = SelectedObjectsProperty.GetValue(obj) as System.Collections.IEnumerable;
            if (list == null)
            {
                return null;
            }

            var guids = new List<string>();
            foreach (var item in list)
            {
                if (item is IGH_DocumentObject docObj)
                {
                    guids.Add(docObj.InstanceGuid.ToString());
                }
            }

            return guids.Count > 0 ? guids : null;
        }
    }
}
