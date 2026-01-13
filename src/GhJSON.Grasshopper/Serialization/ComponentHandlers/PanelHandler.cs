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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Serialization.DataTypes;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_Panel components.
    /// Serializes panel text, color, and bounds.
    /// </summary>
    public class PanelHandler : ComponentHandlerBase
    {
        /// <summary>
        /// Known GUID for GH_Panel component.
        /// </summary>
        public static readonly Guid PanelGuid = new Guid("59e0b89a-e487-49f8-bab8-b5bab16be14c");

        public PanelHandler()
            : base(new[] { PanelGuid }, new[] { typeof(GH_Panel) })
        {
        }

        /// <inheritdoc/>
        public override int Priority => 100;

        /// <inheritdoc/>
        public override bool CanHandle(IGH_DocumentObject obj) => obj is GH_Panel;

        /// <inheritdoc/>
        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_Panel panel)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (panel text)
            state.Value = panel.UserText;
            hasState = true;

            // Extract locked state
            if (panel.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state (via IGH_PreviewObject)
            if (panel is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            // Extract panel color as ARGB string format per schema
            try
            {
                var panelColor = panel.Properties?.Colour;
                if (panelColor.HasValue)
                {
                    var c = panelColor.Value;
                    state.Color = $"{c.A},{c.R},{c.G},{c.B}";
                    hasState = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PanelHandler] Error extracting color: {ex.Message}");
            }

            // Extract panel bounds as WxH string format per schema
            try
            {
                var bounds = panel.Attributes?.Bounds;
                if (bounds.HasValue && !bounds.Value.IsEmpty)
                {
                    state.Bounds = $"{bounds.Value.Width}x{bounds.Value.Height}";
                    hasState = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PanelHandler] Error extracting bounds: {ex.Message}");
            }

            // Extract multiline mode
            try
            {
                if (panel.Properties?.Multiline == true)
                {
                    state.Multiline = true;
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract wrap mode
            try
            {
                if (panel.Properties?.Wrap == true)
                {
                    state.Wrap = true;
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract draw indices
            try
            {
                if (panel.Properties?.DrawIndices == true)
                {
                    state.DrawIndices = true;
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract draw paths
            try
            {
                if (panel.Properties?.DrawPaths == true)
                {
                    state.DrawPaths = true;
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract font
            try
            {
                var font = panel.Properties?.Font;
                if (font != null)
                {
                    var fontDict = new Dictionary<string, object>();

                    if (!string.Equals(font.Name, "Courier New", StringComparison.OrdinalIgnoreCase))
                    {
                        fontDict["name"] = font.Name;
                    }

                    if (font.Bold)
                    {
                        fontDict["bold"] = true;
                    }

                    if (font.Italic)
                    {
                        fontDict["italic"] = true;
                    }

                    // Always include size when emitting font.
                    // If the only thing we have is the default family/style and size, we omit the entire font object.
                    if (fontDict.Count > 0)
                    {
                        fontDict["size"] = font.Size;
                        state.Font = fontDict;
                        hasState = true;
                    }
                }
            }
            catch
            {
            }

            // Extract alignment (store as int per GH enum)
            try
            {
                var align = panel.Properties?.Alignment;
                if (align.HasValue)
                {
                    var alignmentValue = (int)align.Value;
                    if (alignmentValue != 0)
                    {
                        state.Alignment = alignmentValue;
                        hasState = true;
                    }
                }
            }
            catch
            {
            }

            // Extract special codes
            try
            {
                if (panel.Properties?.SpecialCodes == true)
                {
                    state.SpecialCodes = true;
                    hasState = true;
                }
            }
            catch
            {
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_Panel panel || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                panel.Locked = state.Locked.Value;
            }

            // Apply hidden state (via IGH_PreviewObject)
            if (state.Hidden.HasValue && panel is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // Apply color from ARGB string format per schema
            if (!string.IsNullOrEmpty(state.Color))
            {
                try
                {
                    var parts = state.Color.Split(',');
                    if (parts.Length == 4 &&
                        int.TryParse(parts[0], out var a) &&
                        int.TryParse(parts[1], out var r) &&
                        int.TryParse(parts[2], out var g) &&
                        int.TryParse(parts[3], out var b))
                    {
                        panel.Properties.Colour = Color.FromArgb(a, r, g, b);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error applying color: {ex.Message}");
                }
            }

            // Apply bounds from WxH string format per schema
            if (!string.IsNullOrEmpty(state.Bounds))
            {
                try
                {
                    var parts = state.Bounds.Split('x');
                    if (parts.Length == 2 &&
                        float.TryParse(parts[0], out var width) &&
                        float.TryParse(parts[1], out var height))
                    {
                        var attr = panel.Attributes;
                        if (attr != null)
                        {
                            attr.Bounds = new RectangleF(attr.Bounds.X, attr.Bounds.Y, width, height);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error applying bounds: {ex.Message}");
                }
            }

            // Apply multiline
            if (state.Multiline.HasValue)
            {
                panel.Properties.Multiline = state.Multiline.Value;
            }

            // Apply wrap
            if (state.Wrap.HasValue)
            {
                panel.Properties.Wrap = state.Wrap.Value;
            }

            // Apply draw indices
            if (state.DrawIndices.HasValue)
            {
                panel.Properties.DrawIndices = state.DrawIndices.Value;
            }

            // Apply draw paths
            if (state.DrawPaths.HasValue)
            {
                panel.Properties.DrawPaths = state.DrawPaths.Value;
            }

            // Apply font
            if (state.Font != null)
            {
                try
                {
                    var name = state.Font.TryGetValue("name", out var n) ? n?.ToString() : null;
                    var size = state.Font.TryGetValue("size", out var s) && float.TryParse(s?.ToString(), out var fs) ? fs : (float?)null;
                    var bold = state.Font.TryGetValue("bold", out var b) && bool.TryParse(b?.ToString(), out var fb) && fb;
                    var italic = state.Font.TryGetValue("italic", out var i) && bool.TryParse(i?.ToString(), out var fi) && fi;

                    if (!string.IsNullOrEmpty(name) && size.HasValue)
                    {
                        var style = System.Drawing.FontStyle.Regular;
                        if (bold) style |= System.Drawing.FontStyle.Bold;
                        if (italic) style |= System.Drawing.FontStyle.Italic;
                        panel.Properties.Font = new System.Drawing.Font(name, size.Value, style);
                    }
                }
                catch
                {
                }
            }

            // Apply alignment
            if (state.Alignment.HasValue)
            {
                try
                {
                    panel.Properties.Alignment = (GH_Panel.Alignment)state.Alignment.Value;
                }
                catch
                {
                }
            }

            // Apply special codes
            if (state.SpecialCodes.HasValue)
            {
                panel.Properties.SpecialCodes = state.SpecialCodes.Value;
            }

            // Apply value
            if (state.Value != null)
            {
                panel.UserText = state.Value.ToString();
            }
        }
    }
}
