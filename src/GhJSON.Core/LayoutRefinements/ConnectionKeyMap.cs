/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Maps GhJSON connection endpoint ids to the same stable layout keys the layout
    /// engine emits, so id-only components (no InstanceGuid) participate in every
    /// refinement pass through one consistent mapping.
    /// </summary>
    internal static class ConnectionKeyMap
    {
        /// <summary>
        /// Builds the component-id → layout-key map for a document. Components without
        /// an <see cref="GhJsonComponent.Id"/> cannot be addressed by connections and are
        /// skipped.
        /// </summary>
        public static Dictionary<int, Guid> Build(GhJsonDocument document)
        {
            var map = new Dictionary<int, Guid>();
            foreach (var component in document.Components)
            {
                if (component.Id.HasValue)
                {
                    map[component.Id.Value] = GhJson.GetLayoutKey(component);
                }
            }

            return map;
        }
    }
}
