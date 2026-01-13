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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for GH_ButtonObject components.
    /// Serializes button expressions for normal and pressed states.
    /// </summary>
    public class ButtonHandler : ComponentHandlerBase
    {
        /// <summary>
        /// Known GUID for GH_ButtonObject component.
        /// </summary>
        public static readonly Guid ButtonGuid = new Guid("8f9cfa8e-8593-4b15-8b39-5c5e8b07a6c8");

        public ButtonHandler()
            : base(new[] { ButtonGuid }, new[] { typeof(GH_ButtonObject) })
        {
        }

        /// <inheritdoc/>
        public override int Priority => 100;

        /// <inheritdoc/>
        public override bool CanHandle(IGH_DocumentObject obj) => obj is GH_ButtonObject;

        /// <inheritdoc/>
        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_ButtonObject btn)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value (button expressions)
            try
            {
                var expNormal = btn.ExpressionNormal;
                var expPressed = btn.ExpressionPressed;

                // Only serialize if not default values
                if (expNormal != "False" || expPressed != "True")
                {
                    state.Value = new Dictionary<string, string>
                    {
                        { "normal", expNormal ?? "False" },
                        { "pressed", expPressed ?? "True" }
                    };
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract locked state
            if (btn.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state (via IGH_PreviewObject)
            if (btn is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_ButtonObject btn || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                btn.Locked = state.Locked.Value;
            }

            // Apply hidden state (via IGH_PreviewObject)
            if (state.Hidden.HasValue && btn is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // Apply value
            if (state.Value != null)
            {
                try
                {
                    // Handle dictionary format
                    if (state.Value is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("normal", out var normal))
                        {
                            btn.ExpressionNormal = normal?.ToString() ?? "False";
                        }
                        if (dict.TryGetValue("pressed", out var pressed))
                        {
                            btn.ExpressionPressed = pressed?.ToString() ?? "True";
                        }
                    }
                    // Handle Dictionary<string, string>
                    else if (state.Value is Dictionary<string, string> strDict)
                    {
                        if (strDict.TryGetValue("normal", out var normal))
                        {
                            btn.ExpressionNormal = normal ?? "False";
                        }
                        if (strDict.TryGetValue("pressed", out var pressed))
                        {
                            btn.ExpressionPressed = pressed ?? "True";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ButtonHandler] Error applying value: {ex.Message}");
                }
            }
        }
    }
}
