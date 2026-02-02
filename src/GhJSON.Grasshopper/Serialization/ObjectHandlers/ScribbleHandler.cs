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
    /// Serializes text content, corners, and font settings.
    /// </summary>
    internal sealed class ScribbleHandler : IObjectHandler
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

                // Serialize corners if available
                if (scribble.Corners != null && scribble.Corners.Length == 4)
                {
                    var corners = new List<string>();
                    foreach (var corner in scribble.Corners)
                    {
                        corners.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1}",
                            corner.X,
                            corner.Y));
                    }

                    scribbleData["corners"] = corners;
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
                    ApplyScribbleData(scribble, scribbleData);
                }
            }
        }

        private static void ApplyScribbleData(GH_Scribble scribble, Dictionary<string, object> data)
        {
            if (data.TryGetValue("text", out var textVal))
            {
                scribble.Text = textVal?.ToString() ?? string.Empty;
            }

            // Apply corner positions from "x,y" string format
            if (data.TryGetValue("corners", out var cornersVal) && cornersVal is List<object> cornersList)
            {
                try
                {
                    var type = scribble.GetType();
                    string[] cornerNames = { "Corner1", "Corner2", "Corner3", "Corner4" };

                    for (int i = 0; i < Math.Min(cornerNames.Length, cornersList.Count); i++)
                    {
                        var cornerStr = cornersList[i]?.ToString();
                        if (!string.IsNullOrEmpty(cornerStr))
                        {
                            var parts = cornerStr.Split(',');
                            if (parts.Length == 2 &&
                                float.TryParse(parts[0], out var x) &&
                                float.TryParse(parts[1], out var y))
                            {
                                var prop = ReflectionCache.GetProperty(type, cornerNames[i]);
                                if (prop != null && prop.CanWrite)
                                {
                                    prop.SetValue(scribble, new PointF(x, y));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine($"[ScribbleHandler] Error applying corners: {ex.Message}");
#endif
                }
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
                    var size = hasSize && float.TryParse(sizeValObj?.ToString(), out var fs) ? fs : 12f;
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
    }
}
