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
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Shared;
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
                // List is guaranteed to exist by orchestrator - find or create settings for this param
                var settings = component.OutputSettings.FirstOrDefault(s => s.ParameterName == param.Name);
                if (settings == null)
                {
                    settings = new GhJsonParameterSettings { ParameterName = param.Name };
                    component.OutputSettings.Add(settings);
                }

                SerializeParamModifiers(param, settings);
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
                var settings = component.OutputSettings?.FirstOrDefault(s => s.ParameterName == param.Name);
                if (settings != null)
                {
                    DeserializeParamModifiers(settings, param);
                }
            }
        }

        private static void SerializeModifiers(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings> settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            // IOIdentificationHandler creates settings in parameter order
            // Simple index matching is sufficient
            int count = Math.Min(parameters.Count, settings.Count);
            for (int i = 0; i < count; i++)
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

            // Reparameterize - available on Param_Curve and Param_Surface
            var reparameterizeProperty = ReflectionCache.GetProperty(param.GetType(), "Reparameterize");
            if (reparameterizeProperty != null)
            {
                var reparameterizeValue = reparameterizeProperty.GetValue(param);
                Debug.WriteLine($"[IOModifiersHandler.Serialize] Param '{param.Name}' Reparameterize value: {reparameterizeValue}");
                if (reparameterizeValue is bool reparameterize && reparameterize)
                {
                    settings.IsReparameterized = true;
                    Debug.WriteLine($"[IOModifiersHandler.Serialize] Set IsReparameterized=true for param '{param.Name}'");
                }
            }

            // Invert - use cached reflection for boolean parameters
            var invertProperty = ReflectionCache.GetProperty(param.GetType(), "Invert");
            if (invertProperty != null)
            {
                var invertValue = invertProperty.GetValue(param);
                if (invertValue is bool invert && invert)
                {
                    settings.IsInverted = true;
                }
            }

            // Unitize - use cached reflection for vector parameters
            var unitizeProperty = ReflectionCache.GetProperty(param.GetType(), "Unitize");
            if (unitizeProperty != null)
            {
                var unitizeValue = unitizeProperty.GetValue(param);
                Debug.WriteLine($"[IOModifiersHandler.Serialize] Param '{param.Name}' Unitize value: {unitizeValue}");
                if (unitizeValue is bool unitize && unitize)
                {
                    settings.IsUnitized = true;
                    Debug.WriteLine($"[IOModifiersHandler.Serialize] Set IsUnitized=true for param '{param.Name}'");
                }
            }

            // IsPrincipal - use cached reflection for GH_Param<T>
            // Note: IsPrincipal returns GH_PrincipalState enum (IsPrincipal, IsNotPrincipal, CannotBePrincipal)
            var isPrincipalProperty = ReflectionCache.GetProperty(param.GetType(), "IsPrincipal");
            if (isPrincipalProperty != null)
            {
                var isPrincipalValue = isPrincipalProperty.GetValue(param);
                Debug.WriteLine($"[IOModifiersHandler.Serialize] Param '{param.Name}' IsPrincipal value: {isPrincipalValue}");
                
                // Check if the enum value toString is "IsPrincipal"
                if (isPrincipalValue != null && isPrincipalValue.ToString() == "IsPrincipal")
                {
                    settings.IsPrincipal = true;
                    Debug.WriteLine($"[IOModifiersHandler.Serialize] Set IsPrincipal=true for param '{param.Name}'");
                }
            }
            else
            {
                Debug.WriteLine($"[IOModifiersHandler.Serialize] IsPrincipal property not found for param '{param.Name}' (type: {param.GetType().Name})");
            }

            // Expression - use cached reflection for GH_Param<T>
            var expressionProperty = ReflectionCache.GetProperty(param.GetType(), "Expression");
            if (expressionProperty != null)
            {
                var expressionValue = expressionProperty.GetValue(param);
                if (expressionValue is string expression && !string.IsNullOrEmpty(expression))
                {
                    settings.Expression = expression;
                }
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

            // Reparameterize - available on Param_Curve and Param_Surface
            if (settings.IsReparameterized == true)
            {
                var reparameterizeProperty = ReflectionCache.GetProperty(param.GetType(), "Reparameterize");
                if (reparameterizeProperty != null && reparameterizeProperty.CanWrite)
                {
                    reparameterizeProperty.SetValue(param, true);
                    Debug.WriteLine($"[IOModifiersHandler.Deserialize] Set Reparameterize=true for param '{param.Name}'");
                }
            }

            // Invert - use cached reflection for boolean parameters
            if (settings.IsInverted == true)
            {
                var invertProperty = ReflectionCache.GetProperty(param.GetType(), "Invert");
                if (invertProperty != null && invertProperty.CanWrite)
                {
                    invertProperty.SetValue(param, true);
                }
            }

            // Unitize - use cached reflection for vector parameters
            if (settings.IsUnitized == true)
            {
                var unitizeProperty = ReflectionCache.GetProperty(param.GetType(), "Unitize");
                if (unitizeProperty != null && unitizeProperty.CanWrite)
                {
                    unitizeProperty.SetValue(param, true);
                    Debug.WriteLine($"[IOModifiersHandler.Deserialize] Set Unitize=true for param '{param.Name}'");
                }
            }

            // IsPrincipal - use SetPrincipal(bool, bool, bool) method on GH_Param<T>
            if (settings.IsPrincipal == true)
            {
                var setPrincipalMethod = ReflectionCache.GetMethod(param.GetType(), "SetPrincipal");
                if (setPrincipalMethod != null)
                {
                    // SetPrincipal(isPrincipal, isNotPrincipal, cannotBePrincipal)
                    setPrincipalMethod.Invoke(param, new object[] { true, false, false });
                    Debug.WriteLine($"[IOModifiersHandler.Deserialize] Set IsPrincipal=true for param '{param.Name}'");
                }
            }

            // Expression - use cached reflection for GH_Param<T>
            if (!string.IsNullOrEmpty(settings.Expression))
            {
                var expressionProperty = ReflectionCache.GetProperty(param.GetType(), "Expression");
                if (expressionProperty != null && expressionProperty.CanWrite)
                {
                    expressionProperty.SetValue(param, settings.Expression);
                }
            }
        }
    }
}
