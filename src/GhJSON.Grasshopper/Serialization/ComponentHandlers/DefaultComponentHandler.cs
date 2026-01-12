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
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Default component handler that provides basic serialization/deserialization
    /// for components without specialized handlers.
    /// </summary>
    public class DefaultComponentHandler : IComponentHandler
    {
        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => Array.Empty<Guid>();

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => Array.Empty<Type>();

        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj) => true;

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj == null)
                return null;

            var state = new ComponentState();
            bool hasState = false;

            // Extract Locked state for active objects
            if (obj is IGH_ActiveObject activeObj && activeObj.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract Hidden state for preview objects
            if (obj is IGH_PreviewObject previewObj && previewObj.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            // Default handler doesn't extract values
            return null;
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj == null || state == null)
                return;

            // Apply locked state
            if (state.Locked.HasValue && obj is IGH_ActiveObject activeObj)
            {
                activeObj.Locked = state.Locked.Value;
            }

            // Apply hidden state
            if (state.Hidden.HasValue && obj is IGH_PreviewObject previewObj)
            {
                previewObj.Hidden = state.Hidden.Value;
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            // Default handler doesn't apply values
        }
    }
}
