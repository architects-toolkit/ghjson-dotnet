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
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for Value List component state.
    /// Serializes list mode and items.
    /// </summary>
    internal sealed class ValueListHandler : IObjectHandler
    {
        private const string ExtensionKey = "valueList";

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_ValueList;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Value List" ||
                   component.ComponentGuid == new Guid("0b59d304-6b5c-49ce-baaf-041dc057adcc");
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is GH_ValueList valueList)
            {
                component.ComponentState ??= new GhJsonComponentState();
                component.ComponentState.Extensions ??= new Dictionary<string, object>();

                var items = new List<Dictionary<string, object>>();
                foreach (var item in valueList.ListItems)
                {
                    items.Add(new Dictionary<string, object>
                    {
                        ["name"] = item.Name,
                        ["expression"] = item.Expression,
                        ["selected"] = item.Selected
                    });
                }

                var valueListData = new Dictionary<string, object>
                {
                    ["listMode"] = valueList.ListMode.ToString(),
                    ["items"] = items
                };

                component.ComponentState.Extensions[ExtensionKey] = valueListData;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is GH_ValueList valueList &&
                component.ComponentState?.Extensions != null &&
                component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData))
            {
                if (extData is Dictionary<string, object> valueListData)
                {
                    ApplyValueListData(valueList, valueListData);
                }
            }
        }

        private static void ApplyValueListData(GH_ValueList valueList, Dictionary<string, object> data)
        {
            if (data.TryGetValue("listMode", out var modeVal))
            {
                if (Enum.TryParse<GH_ValueListMode>(modeVal?.ToString(), out var mode))
                {
                    valueList.ListMode = mode;
                }
            }

            if (data.TryGetValue("items", out var itemsVal) && itemsVal is List<object> items)
            {
                valueList.ListItems.Clear();

                foreach (var itemObj in items)
                {
                    if (itemObj is Dictionary<string, object> itemData)
                    {
                        var name = itemData.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
                        var expression = itemData.TryGetValue("expression", out var e) ? e?.ToString() ?? string.Empty : string.Empty;
                        var selected = itemData.TryGetValue("selected", out var s) && s is bool sel && sel;

                        var listItem = new GH_ValueListItem(name, expression)
                        {
                            Selected = selected
                        };

                        valueList.ListItems.Add(listItem);
                    }
                }
            }
        }
    }
}
