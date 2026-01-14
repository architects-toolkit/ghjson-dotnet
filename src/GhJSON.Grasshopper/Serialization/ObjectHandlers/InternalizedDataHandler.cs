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
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for internalized (persistent) data in parameters.
    /// </summary>
    internal sealed class InternalizedDataHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component || obj is IGH_Param;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is IGH_Component comp)
            {
                SerializeInternalizedData(comp.Params.Input, component.InputSettings);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    SerializeParamData(param, component.OutputSettings[0]);
                }
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            // Internalized data deserialization is handled by specialized handlers
            // This basic handler only serializes the data
        }

        private static void SerializeInternalizedData(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings>? settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            for (var i = 0; i < parameters.Count && i < settings.Count; i++)
            {
                SerializeParamData(parameters[i], settings[i]);
            }
        }

        private static void SerializeParamData(IGH_Param param, GhJsonParameterSettings settings)
        {
            if (param.DataType != GH_ParamData.local)
            {
                return; // No persistent data
            }

            var persistentData = param.VolatileData;
            if (persistentData.IsEmpty)
            {
                return;
            }

            var dataTree = new Dictionary<string, Dictionary<string, string>>();

            foreach (var path in persistentData.Paths)
            {
                var branch = persistentData.get_Branch(path);
                if (branch == null || branch.Count == 0)
                {
                    continue;
                }

                var pathKey = path.ToString();
                var branchData = new Dictionary<string, string>();

                for (var i = 0; i < branch.Count; i++)
                {
                    var goo = branch[i] as IGH_Goo;
                    if (goo == null)
                    {
                        continue;
                    }

                    var itemKey = $"{pathKey}({i})";
                    var serialized = DataTypeRegistry.Serialize(goo.ScriptVariable());

                    if (!string.IsNullOrEmpty(serialized))
                    {
                        branchData[itemKey] = serialized;
                    }
                    else
                    {
                        // Fallback to string representation
                        branchData[itemKey] = $"text:{goo}";
                    }
                }

                if (branchData.Count > 0)
                {
                    dataTree[pathKey] = branchData;
                }
            }

            if (dataTree.Count > 0)
            {
                settings.InternalizedData = dataTree;
            }
        }
    }
}
