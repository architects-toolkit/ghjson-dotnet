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

using System.Collections.Generic;
using System.Linq;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for component runtime messages (errors, warnings, remarks).
    /// </summary>
    internal sealed class RuntimeMessagesHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_ActiveObject;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is IGH_ActiveObject activeObj)
            {
                var errors = activeObj.RuntimeMessages(GH_RuntimeMessageLevel.Error);
                var warnings = activeObj.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
                var remarks = activeObj.RuntimeMessages(GH_RuntimeMessageLevel.Remark);

                if (errors.Any())
                {
                    component.Errors ??= new List<string>(errors);
                }

                if (warnings.Any())
                {
                    component.Warnings ??= new List<string>(warnings);
                }

                if (remarks.Any())
                {
                    component.Remarks ??= new List<string>(remarks);
                }
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            // Runtime messages are generated during execution, not deserialized
        }
    }
}
