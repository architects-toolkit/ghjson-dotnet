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
    /// Handler for GH_BooleanToggle components.
    /// Serializes boolean value state.
    /// </summary>
    public class ToggleHandler : ComponentHandlerBase
    {
        /// <summary>
        /// Known GUID for GH_BooleanToggle component.
        /// </summary>
        public static readonly Guid ToggleGuid = new Guid("2e78987b-9dfb-42a2-8b76-3eba1d739dec");

        public ToggleHandler()
            : base(new[] { ToggleGuid }, new[] { typeof(GH_BooleanToggle) })
        {
        }

        /// <inheritdoc/>
        public override int Priority => 100;

        /// <inheritdoc/>
        public override bool CanHandle(IGH_DocumentObject obj) => obj is GH_BooleanToggle;

        /// <inheritdoc/>
        public override ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not GH_BooleanToggle toggle)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract value
            state.Value = toggle.Value;
            hasState = true;

            // Extract locked state
            if (toggle.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state (via IGH_PreviewObject)
            if (toggle is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public override void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not GH_BooleanToggle toggle || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue)
            {
                toggle.Locked = state.Locked.Value;
            }

            // Apply hidden state (via IGH_PreviewObject)
            if (state.Hidden.HasValue && toggle is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }

            // Apply value
            if (state.Value != null)
            {
                try
                {
                    if (state.Value is bool boolVal)
                    {
                        toggle.Value = boolVal;
                    }
                    else if (bool.TryParse(state.Value.ToString(), out var parsed))
                    {
                        toggle.Value = parsed;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ToggleHandler] Error applying value: {ex.Message}");
                }
            }
        }
    }
}
