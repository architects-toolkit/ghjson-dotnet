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
using System.Linq;
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using CoreValueListItem = GhJSON.Core.Models.Components.ValueListItem;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_ValueList components.
    /// Serializes list items, selection, and list mode.
    /// </summary>
    public class ValueListHandler : ComponentHandlerBase
    {
        /// <summary>
        /// Known GUID for GH_ValueList component.
        /// </summary>
        public static readonly Guid ValueListGuid = new Guid("f31d8d7a-7536-4ac8-9c96-fde6ecda4d0a");

        public ValueListHandler()
            : base(new[] { ValueListGuid }, new[] { typeof(GH_ValueList) })
        {
        }

        /// <inheritdoc/>
        public override int Priority => 100;

        /// <inheritdoc/>
        public override bool CanHandle(IGH_DocumentObject obj) => obj is GH_ValueList;

        /// <inheritdoc/>
        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_ValueList valueList)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (selected item name)
            try
            {
                state.Value = valueList.FirstSelectedItem?.Name;
                if (state.Value != null)
                {
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract locked state
            if (valueList.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state
            if (valueList.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            // Extract list mode
            try
            {
                var listMode = valueList.ListMode.ToString();
                if (!string.IsNullOrEmpty(listMode))
                {
                    state.ListMode = listMode;
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract list items
            try
            {
                if (valueList.ListItems != null && valueList.ListItems.Count > 0)
                {
                    var items = new List<CoreValueListItem>();
                    foreach (var item in valueList.ListItems)
                    {
                        items.Add(new CoreValueListItem
                        {
                            Name = item.Name,
                            Expression = item.Expression,
                            Selected = item.Selected
                        });
                    }

                    state.ListItems = items;
                    hasState = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ValueListHandler] Error extracting list items: {ex.Message}");
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_ValueList valueList || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                valueList.Locked = state.Locked.Value;
            }

            // Apply hidden state
            if (state.Hidden.HasValue)
            {
                valueList.Hidden = state.Hidden.Value;
            }

            // Apply list mode first (before list items)
            if (!string.IsNullOrEmpty(state.ListMode))
            {
                try
                {
                    if (Enum.TryParse<GH_ValueListMode>(state.ListMode, true, out var mode))
                    {
                        valueList.ListMode = mode;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ValueListHandler] Error applying list mode: {ex.Message}");
                }
            }

            // Apply list items
            if (state.ListItems != null && state.ListItems.Count > 0)
            {
                try
                {
                    valueList.ListItems.Clear();
                    int firstSelectedIndex = -1;
                    int index = 0;

                    foreach (var item in state.ListItems)
                    {
                        if (!string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Expression))
                        {
                            var ghItem = new GH_ValueListItem(item.Name, item.Expression)
                            {
                                Selected = item.Selected
                            };
                            valueList.ListItems.Add(ghItem);

                            if (item.Selected && firstSelectedIndex == -1)
                            {
                                firstSelectedIndex = index;
                            }

                            index++;
                        }
                    }

                    // Ensure at least one item is selected for non-CheckList modes
                    bool anySelected = valueList.ListItems.Any(it => it.Selected);
                    if (!anySelected && valueList.ListItems.Count > 0)
                    {
                        valueList.SelectItem(0);
                    }
                    else if (anySelected && valueList.ListMode != GH_ValueListMode.CheckList)
                    {
                        int idxSel = firstSelectedIndex >= 0 ? firstSelectedIndex : valueList.ListItems.FindIndex(it => it.Selected);
                        if (idxSel < 0) idxSel = 0;
                        valueList.SelectItem(idxSel);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ValueListHandler] Error applying list items: {ex.Message}");
                }
            }

            if (state.Value != null)
            {
                // Apply single value selection for non-CheckList modes
                // (selections are handled via ListItems[].Selected in ApplyState for CheckList)
                if (valueList.ListMode != GH_ValueListMode.CheckList)
                {
                    var valueName = state.Value.ToString();
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        try
                        {
                            for (int i = 0; i < valueList.ListItems.Count; i++)
                            {
                                if (valueList.ListItems[i].Name == valueName)
                                {
                                    valueList.SelectItem(i);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ValueListHandler] Error applying value '{valueName}': {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
