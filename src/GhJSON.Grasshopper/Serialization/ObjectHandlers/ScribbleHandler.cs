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
    /// Serializes text content, rotation angle, corners (relative to pivot), and font settings.
    /// Deserialization mirrors native GH_Scribble.Read() via reflection to preserve rotation.
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

                    // Store rotation angle (degrees) from A→B direction for clarity.
                    var dx = (double)(b.X - a.X);
                    var dy = (double)(b.Y - a.Y);
                    var angleDeg = Math.Atan2(dy, dx) * (180.0 / Math.PI);
                    scribbleData["rotation"] = Math.Round(angleDeg, 6);
                }

                // Serialize font settings as separate fields
                if (scribble.Font != null)
                {
                    scribbleData["fontFamily"] = scribble.Font.FontFamily.Name;
                    scribbleData["fontSize"] = scribble.Font.Size;
                    scribbleData["bold"] = scribble.Font.Bold;
                    scribbleData["italic"] = scribble.Font.Italic;
                }

                component.ComponentState.Extensions[ExtensionKey] = scribbleData;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            // All work is deferred to PostPlacement so that the pivot is finalized first.
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
                    // Mirror native GH_Scribble.Read() via reflection:
                    // 1. Set m_corners field directly (with rotation)
                    // 2. Set m_text field directly (bypass property setter)
                    // 3. Set m_font field directly (bypass property setter)
                    // 4. Invoke RecomputeLayout() once (preserves rotation from corners)
                    // This avoids premature RecomputeLayout calls from property setters
                    // that would reset corners to axis-aligned.
                    ApplyViaReflection(scribble, scribbleData);
                }
            }
        }

        /// <summary>
        /// Applies text, font, and corners via reflection to mirror native GH_Scribble.Read().
        /// This ensures rotation is preserved by setting all fields before any RecomputeLayout call.
        /// </summary>
        private static void ApplyViaReflection(GH_Scribble scribble, Dictionary<string, object> data)
        {
            try
            {
                var scribbleType = typeof(GH_Scribble);

                // 1. Build rotated corners from stored offsets + current pivot
                var corners = BuildAbsoluteCorners(scribble, data);
                if (corners != null)
                {
                    var cornersField = ReflectionCache.GetField(scribbleType, "m_corners");
                    if (cornersField != null)
                    {
                        cornersField.SetValue(scribble, corners);
#if DEBUG
                        Debug.WriteLine($"[ScribbleHandler] Set m_corners via reflection: A=({corners[0].X:F1},{corners[0].Y:F1}), B=({corners[1].X:F1},{corners[1].Y:F1})");
#endif
                    }
                }

                // 2. Set m_text directly (bypasses Text setter which triggers RecomputeLayout)
                if (data.TryGetValue("text", out var textVal))
                {
                    var textField = ReflectionCache.GetField(scribbleType, "m_text");
                    if (textField != null)
                    {
                        textField.SetValue(scribble, textVal?.ToString() ?? string.Empty);
                    }
                }

                // 3. Set m_font directly (bypasses Font setter which triggers RecomputeLayout)
                var font = BuildFont(data);
                if (font != null)
                {
                    var fontField = ReflectionCache.GetField(scribbleType, "m_font");
                    if (fontField != null)
                    {
                        fontField.SetValue(scribble, font);
                    }
                }

                // 4. Invoke RecomputeLayout() once — preserves rotation direction from corners,
                //    recalculates corner distances to match the actual text size.
                var recomputeMethod = ReflectionCache.GetMethod(scribbleType, "RecomputeLayout");
                if (recomputeMethod != null)
                {
                    recomputeMethod.Invoke(scribble, null);
#if DEBUG
                    Debug.WriteLine("[ScribbleHandler] Invoked RecomputeLayout via reflection");
#endif
                }

                // 5. Expire layout so rendering picks up the changes
                scribble.Attributes?.ExpireLayout();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ScribbleHandler] Error in ApplyViaReflection: {ex.Message}");
#endif
                // Fallback: apply text and font via property setters (rotation may be lost)
                ApplyTextAndFontFallback(scribble, data);
            }
        }

        /// <summary>
        /// Builds absolute corner positions from stored offsets relative to the current pivot.
        /// Returns a 4-element PointF array [A, B, C, D], or null if corners are not available.
        /// </summary>
        private static PointF[]? BuildAbsoluteCorners(GH_Scribble scribble, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("corners", out var cornersVal))
            {
                return null;
            }

            // Support both List<object> (from JSON round-trip) and List<string> (in-memory)
            List<string>? cornerStrings = null;
            if (cornersVal is List<object> objList && objList.Count >= 3)
            {
                cornerStrings = new List<string>(objList.Count);
                foreach (var item in objList)
                {
                    cornerStrings.Add(item?.ToString() ?? string.Empty);
                }
            }
            else if (cornersVal is List<string> strList && strList.Count >= 3)
            {
                cornerStrings = strList;
            }

            if (cornerStrings == null || cornerStrings.Count < 3)
            {
                return null;
            }

            var pivot = scribble.Attributes?.Pivot ?? PointF.Empty;

            // 3 stored corners [A, B, D] as offsets relative to pivot.
            var a = ParsePoint(cornerStrings[0]);
            var b = ParsePoint(cornerStrings[1]);
            var d = ParsePoint(cornerStrings[2]);

            var pA = new PointF(pivot.X + a.X, pivot.Y + a.Y);
            var pB = new PointF(pivot.X + b.X, pivot.Y + b.Y);
            var pD = new PointF(pivot.X + d.X, pivot.Y + d.Y);

            // Corner C is derived via parallelogram rule: C = B + D - A
            var pC = new PointF(pB.X + pD.X - pA.X, pB.Y + pD.Y - pA.Y);

            return new PointF[] { pA, pB, pC, pD };
        }

        /// <summary>
        /// Builds a Font from serialized bold/italic fields.
        /// </summary>
        private static Font? BuildFont(Dictionary<string, object> data)
        {
            bool hasFamily = data.TryGetValue("fontFamily", out var familyVal);
            bool hasSize = data.TryGetValue("fontSize", out var sizeValObj);

            if (!hasFamily && !hasSize)
            {
                return null;
            }

            try
            {
                var family = familyVal?.ToString() ?? "Arial";
                var size = hasSize && float.TryParse(sizeValObj?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fs) ? fs : 12f;

                // Build style from separate bold/italic fields
                FontStyle style = FontStyle.Regular;
                if (data.TryGetValue("bold", out var boldVal) && boldVal is bool bBold && bBold)
                {
                    style |= FontStyle.Bold;
                }

                if (data.TryGetValue("italic", out var italicVal) && italicVal is bool bItalic && bItalic)
                {
                    style |= FontStyle.Italic;
                }

                return new Font(family, size, style);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ScribbleHandler] Error building font: {ex.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Fallback: apply text and font via property setters if reflection fails.
        /// Rotation may be lost, but at least text and font are correct.
        /// </summary>
        private static void ApplyTextAndFontFallback(GH_Scribble scribble, Dictionary<string, object> data)
        {
            if (data.TryGetValue("text", out var textVal))
            {
                scribble.Text = textVal?.ToString() ?? string.Empty;
            }

            var font = BuildFont(data);
            if (font != null)
            {
                scribble.Font = font;
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
    }
}
