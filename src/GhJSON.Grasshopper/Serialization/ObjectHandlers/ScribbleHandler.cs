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
using System.Globalization;
using GhJSON.Core.SchemaModels;
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
        private const string ExtensionKey = "scribble";

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

            // Font deserialization would be handled here if needed
        }
    }
}
