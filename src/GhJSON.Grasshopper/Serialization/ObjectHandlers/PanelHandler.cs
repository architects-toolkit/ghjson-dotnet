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
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Serialization.DataTypes;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for Panel component state.
    /// Serializes text content, multiline, wrap, and drawing options.
    /// </summary>
    internal sealed class PanelHandler : IObjectHandler
    {
        private const string ExtensionKey = "gh.panel";

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_Panel;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Panel" ||
                   component.ComponentGuid == new Guid("59e0b89a-e487-49f8-bab8-b5bab16be14c");
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is GH_Panel panel)
            {
                component.ComponentState ??= new GhJsonComponentState();
                component.ComponentState.Extensions ??= new Dictionary<string, object>();

                var panelData = new Dictionary<string, object>
                {
                    ["text"] = panel.UserText,
                    ["multiline"] = panel.Properties.Multiline,
                    ["wrap"] = panel.Properties.Wrap,
                    ["drawIndices"] = panel.Properties.DrawIndices,
                    ["drawPaths"] = panel.Properties.DrawPaths
                };

                // Serialize color using ColorSerializer (argb:A,R,G,B)
                try
                {
                    var panelColor = panel.Properties?.Colour;
                    if (panelColor.HasValue)
                    {
                        var serializer = new ColorSerializer();
                        panelData["color"] = serializer.Serialize(panelColor.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error serializing color: {ex.Message}");
                }

                // Serialize bounds as WxH string format
                try
                {
                    var bounds = panel.Attributes?.Bounds;
                    if (bounds.HasValue && !bounds.Value.IsEmpty)
                    {
                        panelData["bounds"] = $"{bounds.Value.Width}x{bounds.Value.Height}";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error serializing bounds: {ex.Message}");
                }

                // Serialize alignment
                try
                {
                    var align = panel.Properties?.Alignment;
                    if (align.HasValue)
                    {
                        var alignmentValue = (int)align.Value;
                        if (alignmentValue != 0)
                        {
                            panelData["alignment"] = alignmentValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error serializing alignment: {ex.Message}");
                }

                // Serialize special codes
                try
                {
                    if (panel.Properties?.SpecialCodes == true)
                    {
                        panelData["specialCodes"] = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error serializing special codes: {ex.Message}");
                }

                // Serialize font
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

                        if (fontDict.Count > 0)
                        {
                            fontDict["size"] = font.Size;
                            panelData["font"] = fontDict;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error serializing font: {ex.Message}");
                }

                component.ComponentState.Extensions[ExtensionKey] = panelData;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is GH_Panel panel &&
                component.ComponentState?.Extensions != null &&
                component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData))
            {
                if (extData is Dictionary<string, object> panelData)
                {
                    ApplyPanelData(panel, panelData);
                }
            }
        }

        private static void ApplyPanelData(GH_Panel panel, Dictionary<string, object> data)
        {
            if (data.TryGetValue("text", out var textVal))
            {
                panel.UserText = textVal?.ToString() ?? string.Empty;
            }

            if (data.TryGetValue("multiline", out var multiVal) && multiVal is bool multi)
            {
                panel.Properties.Multiline = multi;
            }

            if (data.TryGetValue("wrap", out var wrapVal) && wrapVal is bool wrap)
            {
                panel.Properties.Wrap = wrap;
            }

            if (data.TryGetValue("drawIndices", out var indicesVal) && indicesVal is bool indices)
            {
                panel.Properties.DrawIndices = indices;
            }

            if (data.TryGetValue("drawPaths", out var pathsVal) && pathsVal is bool paths)
            {
                panel.Properties.DrawPaths = paths;
            }

            // Apply color using ColorSerializer; fallback to legacy comma format
            if (data.TryGetValue("color", out var colorVal))
            {
                try
                {
                    var colorString = colorVal?.ToString();
                    var serializer = new ColorSerializer();

                    if (!string.IsNullOrWhiteSpace(colorString))
                    {
                        if (serializer.IsValid(colorString))
                        {
                            panel.Properties.Colour = serializer.Deserialize(colorString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error applying color: {ex.Message}");
                }
            }

            // Apply bounds from WxH string format
            if (data.TryGetValue("bounds", out var boundsVal))
            {
                try
                {
                    var parts = boundsVal?.ToString()?.Split('x');
                    if (parts?.Length == 2 &&
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

            // Apply alignment
            if (data.TryGetValue("alignment", out var alignVal))
            {
                try
                {
                    if (int.TryParse(alignVal?.ToString(), out var alignmentValue))
                    {
                        panel.Properties.Alignment = (GH_Panel.Alignment)alignmentValue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error applying alignment: {ex.Message}");
                }
            }

            // Apply special codes
            if (data.TryGetValue("specialCodes", out var specialVal) && specialVal is bool special)
            {
                panel.Properties.SpecialCodes = special;
            }

            // Apply font
            if (data.TryGetValue("font", out var fontVal) && fontVal is Dictionary<string, object> fontDict)
            {
                try
                {
                    var name = fontDict.TryGetValue("name", out var n) ? n?.ToString() : null;
                    var size = fontDict.TryGetValue("size", out var s) && float.TryParse(s?.ToString(), out var fs) ? fs : (float?)null;
                    var bold = fontDict.TryGetValue("bold", out var b) && bool.TryParse(b?.ToString(), out var fb) && fb;
                    var italic = fontDict.TryGetValue("italic", out var i) && bool.TryParse(i?.ToString(), out var fi) && fi;

                    if (!string.IsNullOrEmpty(name) && size.HasValue)
                    {
                        var style = FontStyle.Regular;
                        if (bold) style |= FontStyle.Bold;
                        if (italic) style |= FontStyle.Italic;
                        panel.Properties.Font = new Font(name, size.Value, style);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PanelHandler] Error applying font: {ex.Message}");
                }
            }
        }
    }
}
