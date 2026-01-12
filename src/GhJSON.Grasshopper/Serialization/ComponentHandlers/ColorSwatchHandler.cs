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
    /// Handler for GH_ColourSwatch components.
    /// Serializes swatch color value.
    /// </summary>
    public class ColorSwatchHandler : IComponentHandler
    {
        /// <summary>
        /// Known GUID for GH_ColourSwatch component.
        /// </summary>
        public static readonly Guid ColorSwatchGuid = new Guid("9c53bac0-ba66-40bd-8154-ce9829b9db1a");

        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => new[] { ColorSwatchGuid };

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => new[] { typeof(GH_ColourSwatch) };

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj) => obj is GH_ColourSwatch;

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_ColourSwatch swatch)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (color as rgba format)
            var value = ExtractValue(obj);
            if (value != null)
            {
                state.Value = value;
                hasState = true;
            }

            // Also store in AdditionalProperties for backward compatibility
            try
            {
                state.AdditionalProperties ??= new Dictionary<string, object>();
                state.AdditionalProperties["color"] = DataTypeSerializer.Serialize(swatch.SwatchColour);
                hasState = true;
            }
            catch
            {
            }

            // Extract locked state
            if (swatch.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state (via IGH_PreviewObject)
            if (swatch is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            if (obj is not GH_ColourSwatch swatch)
                return null;

            try
            {
                var c = swatch.SwatchColour;
                return $"rgba:{c.R},{c.G},{c.B},{c.A}";
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_ColourSwatch swatch || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                swatch.Locked = state.Locked.Value;
            }

            // Apply hidden state (via IGH_PreviewObject)
            if (state.Hidden.HasValue && swatch is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // Apply color from AdditionalProperties (preferred)
            if (state.AdditionalProperties != null &&
                state.AdditionalProperties.TryGetValue("color", out var colorObj) &&
                colorObj is string colorStr &&
                DataTypeSerializer.TryDeserialize("Color", colorStr, out var deserialized) &&
                deserialized is Color serializedColor)
            {
                swatch.SwatchColour = serializedColor;
            }
            // Apply value (legacy rgba format)
            else if (state.Value != null)
            {
                ApplyValue(obj, state.Value);
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            if (obj is not GH_ColourSwatch swatch || value == null)
                return;

            var str = value.ToString();
            if (string.IsNullOrEmpty(str))
                return;

            try
            {
                // Try argb format first (DataTypeSerializer)
                if (DataTypeSerializer.TryDeserializeFromPrefix(str, out var colorObj) &&
                    colorObj is Color c)
                {
                    swatch.SwatchColour = c;
                    return;
                }

                // Try legacy rgba format
                var color = ParseRgbaColor(str);
                if (color.HasValue)
                {
                    swatch.SwatchColour = color.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ColorSwatchHandler] Error applying value: {ex.Message}");
            }
        }

        private static Color? ParseRgbaColor(string? colorStr)
        {
            if (string.IsNullOrEmpty(colorStr))
                return null;

            try
            {
                // Parse format "rgba:R,G,B,A"
                if (colorStr.StartsWith("rgba:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = colorStr.Substring(5).Split(',');
                    if (parts.Length >= 3)
                    {
                        var r = int.Parse(parts[0].Trim());
                        var g = int.Parse(parts[1].Trim());
                        var b = int.Parse(parts[2].Trim());
                        var a = parts.Length >= 4 ? int.Parse(parts[3].Trim()) : 255;
                        return Color.FromArgb(a, r, g, b);
                    }
                }

                // Try HTML color
                return ColorTranslator.FromHtml(colorStr);
            }
            catch
            {
                return null;
            }
        }
    }
}
