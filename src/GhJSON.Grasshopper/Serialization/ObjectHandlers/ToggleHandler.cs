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
    internal sealed class ToggleHandler : IObjectHandler
    {
        private const string ExtensionKey = "gh.toggle";

        private static readonly Guid ToggleGuid = new Guid("2e78987b-9dfb-42a2-8b76-3923ac8bd91a");

        public int Priority => 100;

        public string? SchemaExtensionUrl => null;

        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_BooleanToggle;
        }

        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Boolean Toggle" ||
                component.Name == "Toggle" ||
                component.ComponentGuid == ToggleGuid;
        }

        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not GH_BooleanToggle toggle)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var toggleData = new Dictionary<string, object>
            {
                ["value"] = toggle.Value,
            };

            component.ComponentState.Extensions[ExtensionKey] = toggleData;
        }

        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not GH_BooleanToggle toggle ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> toggleData)
            {
                return;
            }

            if (toggleData.TryGetValue("value", out var value) && value is bool b)
            {
                toggle.Value = b;
            }
            else if (toggleData.TryGetValue("value", out value) && bool.TryParse(value?.ToString(), out var parsed))
            {
                toggle.Value = parsed;
            }
        }
    }
}
