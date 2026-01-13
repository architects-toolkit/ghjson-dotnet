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
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for component identification properties (name, componentGuid, instanceGuid).
    /// </summary>
    internal sealed class IdentificationHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return true; // Handles all document objects
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return true; // Handles all components
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            component.Name ??= obj.Name;
            component.NickName ??= obj.NickName != obj.Name ? obj.NickName : null;
            component.ComponentGuid ??= obj.ComponentGuid;
            component.InstanceGuid ??= obj.InstanceGuid;

            // Get library/category if available
            if (component.Library == null && obj is IGH_ActiveObject activeObj)
            {
                component.Library = activeObj.Category;
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            // NickName can be set after instantiation
            if (!string.IsNullOrEmpty(component.NickName))
            {
                obj.NickName = component.NickName;
            }
        }
    }
}
