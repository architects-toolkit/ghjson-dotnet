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
using System.Globalization;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for Scribble component state.
    /// Serializes text content, corners (relative to pivot), and font settings.
    /// Corners are stored as relative positions to allow proper offset handling during placement.
    /// </summary>
    internal sealed class ScribbleHandler : IObjectHandler, IPostPlacementHandler
    {
        private const string ExtensionKey = "gh.scribble";

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_Scribble;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Scribble" ||
                   component.ComponentGuid == new Guid("7f5c6c55-f846-4a08-9c9a-cfdc285cc6fe");
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is GH_Scribble scribble)
            {
                component.ComponentState ??= new GhJsonComponentState();
                component.ComponentState.Extensions ??= new Dictionary<string, object>();

                var scribbleData = new Dictionary<string, object>
                {
                    ["text"] = scribble.Text
                };

                // Serialize 3 corners (A, B, D) as offsets relative to pivot.
                // Corner C is derived as B + D - A (parallelogram rule).
                if (scribble.Corners != null && scribble.Corners.Length == 4)
                {
                    var pivot = scribble.Attributes?.Pivot ?? scribble.Corners[0];
                    var a = scribble.Corners[0];
                    var b = scribble.Corners[1];
                    var d = scribble.Corners[3];

                    scribbleData["corners"] = new List<string>
                    {
                        FormatPoint(a.X - pivot.X, a.Y - pivot.Y),
                        FormatPoint(b.X - pivot.X, b.Y - pivot.Y),
                        FormatPoint(d.X - pivot.X, d.Y - pivot.Y)
                    };
                }

                // Serialize font settings
                if (scribble.Font != null)
                {
                    scribbleData["fontFamily"] = scribble.Font.FontFamily.Name;
                    scribbleData["fontSize"] = scribble.Font.Size;
                    scribbleData["fontStyle"] = scribble.Font.Style.ToString();
                }

                component.ComponentState.Extensions[ExtensionKey] = scribbleData;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is GH_Scribble scribble &&
                component.ComponentState?.Extensions != null &&
                component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData))
            {
                if (extData is Dictionary<string, object> scribbleData)
                {
                    // Apply everything in PostPlacement after the pivot is finalized with offsets.
                    // (Setting Text/Font triggers internal layout recomputation that depends on
                    // the current corner basis, so ordering matters.)
                }
            }
        }

        /// <inheritdoc/>
        public void PostPlacement(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is GH_Scribble scribble &&
                component.ComponentState?.Extensions != null &&
                component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData))
            {
                if (extData is Dictionary<string, object> scribbleData)
                {
                    // Important: establish corner basis (rotation) first, then apply text/font.
                    ApplyCornerBasis(scribble, scribbleData);
                    ApplyTextAndFont(scribble, scribbleData);
                }
            }
        }

        private static void ApplyTextAndFont(GH_Scribble scribble, Dictionary<string, object> data)
        {
            if (data.TryGetValue("text", out var textVal))
            {
                scribble.Text = textVal?.ToString() ?? string.Empty;
            }

            // Apply font
            bool hasFamily = data.TryGetValue("fontFamily", out var familyVal);
            bool hasSize = data.TryGetValue("fontSize", out var sizeValObj);
            bool hasStyle = data.TryGetValue("fontStyle", out var styleValObj);

            if (hasFamily || hasSize || hasStyle)
            {
                try
                {
                    var family = familyVal?.ToString() ?? "Arial";
                    var size = hasSize && float.TryParse(sizeValObj?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fs) ? fs : 12f;
                    var style = hasStyle && Enum.TryParse<FontStyle>(styleValObj?.ToString(), out var parsed) ? parsed : FontStyle.Regular;

                    scribble.Font = new Font(family, size, style);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine($"[ScribbleHandler] Error applying font: {ex.Message}");
#endif
                }
            }
        }

        private static void ApplyCornerBasis(GH_Scribble scribble, Dictionary<string, object> data)
        {
            try
            {
                if (!data.TryGetValue("corners", out var cornersVal) ||
                    cornersVal is not List<object> cornersList ||
                    cornersList.Count < 3)
                {
                    return;
                }

                var pivot = scribble.Attributes?.Pivot ?? PointF.Empty;
                var type = scribble.GetType();

                // 3 corners [A, B, D] relative to pivot.
                // Corner C is derived as B + D - A (parallelogram rule).
                var a = ParsePoint(cornersList[0]);
                var b = ParsePoint(cornersList[1]);
                var d = ParsePoint(cornersList[2]);

                SetCorner(type, scribble, "Corner1", new PointF(pivot.X + a.X, pivot.Y + a.Y));
                SetCorner(type, scribble, "Corner2", new PointF(pivot.X + b.X, pivot.Y + b.Y));
                SetCorner(type, scribble, "Corner4", new PointF(pivot.X + d.X, pivot.Y + d.Y));
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ScribbleHandler] Error applying corners: {ex.Message}");
#endif
            }
        }

        private static string FormatPoint(float x, float y)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", x, y);
        }

        private static PointF ParsePoint(object? value)
        {
            var str = value?.ToString();
            if (string.IsNullOrEmpty(str))
            {
                return PointF.Empty;
            }

            var parts = str.Split(',');
            if (parts.Length != 2)
            {
                return PointF.Empty;
            }

            return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                   float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                ? new PointF(x, y)
                : PointF.Empty;
        }

        private static void SetCorner(Type scribbleType, GH_Scribble scribble, string cornerName, PointF value)
        {
            var prop = ReflectionCache.GetProperty(scribbleType, cornerName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(scribble, value);
#if DEBUG
                Debug.WriteLine($"[ScribbleHandler] Set {cornerName} to ({value.X}, {value.Y})");
#endif
            }
        }
    }
}
