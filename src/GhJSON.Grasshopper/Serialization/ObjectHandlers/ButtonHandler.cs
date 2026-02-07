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
    internal sealed class ButtonHandler : IObjectHandler
    {
        private const string ExtensionKey = "gh.button";

        private static readonly Guid ButtonGuid = new Guid("a8b97322-2d53-47cd-905e-e3a78807825d");

        public int Priority => 100;

        public string? SchemaExtensionUrl => null;

        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is GH_ButtonObject;
        }

        public bool CanHandle(GhJsonComponent component)
        {
            return component.Name == "Button" ||
                   component.ComponentGuid == ButtonGuid;
        }

        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not GH_ButtonObject button)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var buttonData = new Dictionary<string, object>
            {
                ["normal"] = button.ExpressionNormal ?? "False",
                ["pressed"] = button.ExpressionPressed ?? "True",
            };

            component.ComponentState.Extensions[ExtensionKey] = buttonData;
        }

        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not GH_ButtonObject button ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> buttonData)
            {
                return;
            }

            bool expressionsSet = false;

            if (buttonData.TryGetValue("normal", out var normal))
            {
                button.ExpressionNormal = normal?.ToString() ?? "False";
                expressionsSet = true;
            }

            if (buttonData.TryGetValue("pressed", out var pressed))
            {
                button.ExpressionPressed = pressed?.ToString() ?? "True";
                expressionsSet = true;
            }

            // Evaluate the new expressions so the button computes its normal/pressed values.
            // EvaluateExpressions() is the correct API; ExpireSolution() would trigger a full
            // document recompute which is unnecessary and can cause side effects during placement.
            if (expressionsSet)
            {
                button.EvaluateExpressions();
            }
        }
    }
}
