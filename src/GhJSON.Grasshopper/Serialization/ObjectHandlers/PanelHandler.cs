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
using GhJSON.Core.SchemaModels;
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
        private const string ExtensionKey = "panel";

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
        }
    }
}
