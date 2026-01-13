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

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for input/output parameter modifiers (dataMapping, simplify, reverse, etc.).
    /// </summary>
    internal sealed class IOModifiersHandler : IObjectHandler
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
                SerializeModifiers(comp.Params.Input, component.InputSettings);
                SerializeModifiers(comp.Params.Output, component.OutputSettings);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    SerializeParamModifiers(param, component.OutputSettings[0]);
                }
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is IGH_Component comp)
            {
                DeserializeModifiers(component.InputSettings, comp.Params.Input);
                DeserializeModifiers(component.OutputSettings, comp.Params.Output);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    DeserializeParamModifiers(component.OutputSettings[0], param);
                }
            }
        }

        private static void SerializeModifiers(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings>? settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            for (var i = 0; i < parameters.Count && i < settings.Count; i++)
            {
                SerializeParamModifiers(parameters[i], settings[i]);
            }
        }

        private static void SerializeParamModifiers(IGH_Param param, GhJsonParameterSettings settings)
        {
            // Data mapping
            if (param.DataMapping != GH_DataMapping.None)
            {
                settings.DataMapping = param.DataMapping.ToString();
            }

            // Simplify
            if (param.Simplify)
            {
                settings.IsSimplified = true;
            }

            // Reverse
            if (param.Reverse)
            {
                settings.IsReversed = true;
            }
        }

        private static void DeserializeModifiers(
            List<GhJsonParameterSettings>? settings,
            IList<IGH_Param> parameters)
        {
            if (settings == null || parameters == null)
            {
                return;
            }

            foreach (var paramSettings in settings)
            {
                var param = FindParameter(parameters, paramSettings.ParameterName);
                if (param != null)
                {
                    DeserializeParamModifiers(paramSettings, param);
                }
            }
        }

        private static IGH_Param? FindParameter(IList<IGH_Param> parameters, string name)
        {
            foreach (var param in parameters)
            {
                if (param.Name == name)
                {
                    return param;
                }
            }

            return null;
        }

        private static void DeserializeParamModifiers(GhJsonParameterSettings settings, IGH_Param param)
        {
            // Data mapping
            if (!string.IsNullOrEmpty(settings.DataMapping))
            {
                if (System.Enum.TryParse<GH_DataMapping>(settings.DataMapping, out var mapping))
                {
                    param.DataMapping = mapping;
                }
            }

            // Simplify
            if (settings.IsSimplified == true)
            {
                param.Simplify = true;
            }

            // Reverse
            if (settings.IsReversed == true)
            {
                param.Reverse = true;
            }
        }
    }
}
