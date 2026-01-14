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
    /// Handler for input/output parameter identification.
    /// </summary>
    internal sealed class IOIdentificationHandler : IObjectHandler
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
                var inputSettings = component.InputSettings;
                SerializeParameters(comp.Params.Input, ref inputSettings);
                component.InputSettings = inputSettings;

                var outputSettings = component.OutputSettings;
                SerializeParameters(comp.Params.Output, ref outputSettings);
                component.OutputSettings = outputSettings;
            }
            else if (obj is IGH_Param param)
            {
                // Floating parameters have their settings serialized as output settings
                if (component.OutputSettings == null)
                {
                    component.OutputSettings = new List<GhJsonParameterSettings>
                    {
                        CreateParameterSettings(param)
                    };
                }
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is IGH_Component comp)
            {
                DeserializeParameters(component.InputSettings, comp.Params.Input);
                DeserializeParameters(component.OutputSettings, comp.Params.Output);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    ApplyParameterSettings(component.OutputSettings[0], param);
                }
            }
        }

        private static void SerializeParameters(
            IList<IGH_Param> parameters,
            ref List<GhJsonParameterSettings>? settings)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }

            var localSettings = settings ?? new List<GhJsonParameterSettings>();

            foreach (var param in parameters)
            {
                var paramSettings = CreateParameterSettings(param);
                localSettings.Add(paramSettings);
            }

            settings = localSettings;
        }

        private static GhJsonParameterSettings CreateParameterSettings(IGH_Param param)
        {
            var settings = new GhJsonParameterSettings
            {
                ParameterName = param.Name
            };

            if (param.NickName != param.Name)
            {
                settings.NickName = param.NickName;
            }

            return settings;
        }

        private static void DeserializeParameters(
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
                    ApplyParameterSettings(paramSettings, param);
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

        private static void ApplyParameterSettings(GhJsonParameterSettings settings, IGH_Param param)
        {
            if (!string.IsNullOrEmpty(settings.NickName))
            {
                param.NickName = settings.NickName;
            }
        }
    }
}
