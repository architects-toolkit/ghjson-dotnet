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

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_ValueList components.
    /// Serializes list items, selection, and list mode.
    /// </summary>
    public class ValueListHandler : IComponentHandler
    {
        /// <summary>
        /// Known GUID for GH_ValueList component.
        /// </summary>
        public static readonly Guid ValueListGuid = new Guid("f31d8d7a-7536-4ac8-9c96-fde6ecda4d0a");

        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => new[] { ValueListGuid };

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => new[] { typeof(GH_ValueList) };

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj) => obj is GH_ValueList;

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_ValueList valueList)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (selected item name)
            var value = ExtractValue(obj);
            if (value != null)
            {
                state.Value = value;
                hasState = true;
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

            // Extract selected indices (for CheckList mode)
            if (valueList.ListMode == GH_ValueListMode.CheckList)
            {
                try
                {
                    var selectedIndices = new List<int>();
                    for (int i = 0; i < valueList.ListItems.Count; i++)
                    {
                        if (valueList.ListItems[i].Selected)
                        {
                            selectedIndices.Add(i);
                        }
                    }
                    if (selectedIndices.Count > 0)
                    {
                        state.SelectedIndices = selectedIndices;
                        hasState = true;
                    }
                }
                catch
                {
                }
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            if (obj is not GH_ValueList valueList)
                return null;

            try
            {
                return valueList.FirstSelectedItem?.Name;
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
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

            // Apply list mode
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

            // Apply selected indices (for CheckList mode)
            if (valueList.ListMode == GH_ValueListMode.CheckList && state.SelectedIndices != null)
            {
                try
                {
                    for (int i = 0; i < valueList.ListItems.Count; i++)
                    {
                        valueList.ListItems[i].Selected = state.SelectedIndices.Contains(i);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ValueListHandler] Error applying selected indices: {ex.Message}");
                }
            }
            else if (state.Value != null)
            {
                // Apply single value selection for non-CheckList modes
                ApplyValue(obj, state.Value);
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            if (obj is not GH_ValueList valueList || value == null)
                return;

            // Don't apply single value selection for CheckList mode
            // (selections are handled via SelectedIndices in ApplyState)
            if (valueList.ListMode == GH_ValueListMode.CheckList)
                return;

            var valueName = value.ToString();
            if (string.IsNullOrEmpty(valueName))
                return;

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
