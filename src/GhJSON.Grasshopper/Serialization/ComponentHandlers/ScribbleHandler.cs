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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_Scribble components.
    /// Serializes scribble text, font, and corner positions.
    /// </summary>
    public class ScribbleHandler : IComponentHandler
    {
        /// <summary>
        /// Known GUID for GH_Scribble component.
        /// </summary>
        public static readonly Guid ScribbleGuid = new Guid("7f5c6c55-f846-4a08-9c9a-cfdc285cc6fe");

        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => new[] { ScribbleGuid };

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => new[] { typeof(GH_Scribble) };

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj) => obj is GH_Scribble;

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_Scribble scribble)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (scribble text)
            var value = ExtractValue(obj);
            if (value != null)
            {
                state.Value = value;
                hasState = true;
            }

            // Extract locked state (via IGH_ActiveObject)
            if (scribble is IGH_ActiveObject activeObj && activeObj.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract font information
            try
            {
                var font = scribble.Font;
                if (font != null)
                {
                    state.Font = new Dictionary<string, object>
                    {
                        { "family", font.FontFamily.Name },
                        { "size", font.Size },
                        { "style", font.Style.ToString() }
                    };
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract corner positions as array of "x,y" strings per schema
            try
            {
                var corners = new List<string>();
                
                // GH_Scribble has Corner1, Corner2, Corner3, Corner4 properties
                var type = scribble.GetType();
                string[] cornerNames = { "Corner1", "Corner2", "Corner3", "Corner4" };
                
                foreach (var cornerName in cornerNames)
                {
                    var prop = type.GetProperty(cornerName);
                    if (prop != null && prop.CanRead)
                    {
                        var corner = prop.GetValue(scribble);
                        if (corner is PointF pt)
                        {
                            corners.Add($"{pt.X},{pt.Y}");
                        }
                    }
                }

                if (corners.Count > 0)
                {
                    state.Corners = corners;
                    hasState = true;
                }
            }
            catch
            {
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            if (obj is not GH_Scribble scribble)
                return null;

            return scribble.Text;
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_Scribble scribble || state == null)
                return;

            // Apply locked state (via IGH_ActiveObject)
            if (state.Locked.HasValue && scribble is IGH_ActiveObject activeObj)
            {
                activeObj.Locked = state.Locked.Value;
            }

            // Apply font
            if (state.Font != null)
            {
                try
                {
                    var family = state.Font.TryGetValue("family", out var f) ? f?.ToString() : "Arial";
                    var size = state.Font.TryGetValue("size", out var s) && s is float fs ? fs : 12f;
                    var style = state.Font.TryGetValue("style", out var st) && st is string styleStr
                        ? Enum.TryParse<FontStyle>(styleStr, out var parsed) ? parsed : FontStyle.Regular
                        : FontStyle.Regular;

                    scribble.Font = new Font(family ?? "Arial", size, style);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ScribbleHandler] Error applying font: {ex.Message}");
                }
            }

            // Apply corner positions from "x,y" string format per schema
            if (state.Corners != null && state.Corners.Count >= 4)
            {
                try
                {
                    var type = scribble.GetType();
                    string[] cornerNames = { "Corner1", "Corner2", "Corner3", "Corner4" };

                    for (int i = 0; i < Math.Min(cornerNames.Length, state.Corners.Count); i++)
                    {
                        var cornerStr = state.Corners[i];
                        var parts = cornerStr.Split(',');
                        if (parts.Length == 2 &&
                            float.TryParse(parts[0], out var x) &&
                            float.TryParse(parts[1], out var y))
                        {
                            var prop = type.GetProperty(cornerNames[i]);
                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(scribble, new PointF(x, y));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ScribbleHandler] Error applying corners: {ex.Message}");
                }
            }

            // Apply value
            if (state.Value != null)
            {
                ApplyValue(obj, state.Value);
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            if (obj is not GH_Scribble scribble || value == null)
                return;

            scribble.Text = value.ToString();
        }
    }
}
