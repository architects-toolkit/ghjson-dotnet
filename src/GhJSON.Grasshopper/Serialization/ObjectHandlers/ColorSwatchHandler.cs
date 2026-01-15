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
using GhJSON.Grasshopper.Serialization.DataTypes;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    internal sealed class ColorSwatchHandler : IObjectHandler
    {
        private const string ExtensionKey = "gh.colorswatch";

        private static readonly Guid ColorSwatchGuid = new Guid("9c53bac0-ba66-40bd-8154-ce9829b9db1a");

        public int Priority => 100;

        public string? SchemaExtensionUrl => null;

        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_ColourSwatch;
        }

        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Color Swatch" ||
                   component.ComponentGuid == ColorSwatchGuid;
        }

        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not GH_ColourSwatch swatch)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var serializer = new ColorSerializer();
            var swatchData = new Dictionary<string, object>
            {
                ["color"] = serializer.Serialize(swatch.SwatchColour),
            };

            component.ComponentState.Extensions[ExtensionKey] = swatchData;
        }

        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not GH_ColourSwatch swatch ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> swatchData)
            {
                return;
            }

            if (!swatchData.TryGetValue("color", out var colorObj))
            {
                return;
            }

            var colorString = colorObj?.ToString();
            if (string.IsNullOrWhiteSpace(colorString))
            {
                return;
            }

            var serializer = new ColorSerializer();
            if (serializer.IsValid(colorString))
            {
                swatch.SwatchColour = serializer.Deserialize(colorString);
            }
        }
    }
}
