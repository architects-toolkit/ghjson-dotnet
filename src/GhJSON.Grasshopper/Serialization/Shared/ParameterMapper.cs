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
using System.Diagnostics;
using GhJSON.Core.Models.Components;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.Shared
{
    /// <summary>
    /// Handles bidirectional mapping between IGH_Param and ParameterSettings.
    /// Provides consistent parameter conversion for non-script components.
    /// </summary>
    public static class ParameterMapper
    {
        /// <summary>
        /// Extracts parameter settings from a Grasshopper parameter.
        /// </summary>
        /// <param name="param">The parameter to extract from.</param>
        /// <param name="isPrincipal">Whether this parameter is the principal/master input parameter (only valid for inputs).</param>
        /// <returns>Parameter settings or null if no custom settings exist.</returns>
        public static ParameterSettings? ExtractSettings(IGH_Param param, bool isPrincipal = false)
        {
            if (param == null)
                return null;

            var settings = new ParameterSettings
            {
                ParameterName = param.Name
            };

            bool hasSettings = false;

            // Extract NickName if different from Name
            if (!string.IsNullOrEmpty(param.NickName) &&
                !string.Equals(param.Name, param.NickName, StringComparison.Ordinal))
            {
                settings.NickName = param.NickName;
                hasSettings = true;
            }

            // Mark as principal if applicable (only valid for inputs)
            if (isPrincipal && param.Kind == GH_ParamKind.input)
            {
                settings.IsPrincipal = true;
                hasSettings = true;
            }

            // Extract DataMapping (None, Flatten, Graft)
            if (param.DataMapping != GH_DataMapping.None)
            {
                settings.DataMapping = param.DataMapping.ToString();
                hasSettings = true;
            }

            // Extract flattened parameter modifiers (per schema)
            if (param.Reverse)
            {
                settings.Reverse = true;
                hasSettings = true;
            }

            if (param.Simplify)
            {
                settings.Simplify = true;
                hasSettings = true;
            }

            // Extract Invert flag via reflection (for boolean parameters)
            try
            {
                var invertProp = param.GetType().GetProperty("Invert");
                if (invertProp != null && invertProp.CanRead)
                {
                    var invertValue = invertProp.GetValue(param) as bool?;
                    if (invertValue == true)
                    {
                        settings.Invert = true;
                        hasSettings = true;
                    }
                }
            }
            catch
            {
                // Property doesn't exist or can't be read
            }

            // Extract Unitize flag via reflection (for vector parameters)
            try
            {
                var unitizeProp = param.GetType().GetProperty("Unitize");
                if (unitizeProp != null && unitizeProp.CanRead)
                {
                    var unitizeValue = unitizeProp.GetValue(param) as bool?;
                    if (unitizeValue == true)
                    {
                        settings.Unitize = true;
                        hasSettings = true;
                    }
                }
            }
            catch
            {
                // Property doesn't exist or can't be read
            }

            // Extract Required/Optional property (only for inputs)
            if (param.Kind == GH_ParamKind.input)
            {
                try
                {
                    var optionalProp = param.GetType().GetProperty("Optional");
                    if (optionalProp != null && optionalProp.CanRead)
                    {
                        var isOptional = optionalProp.GetValue(param) as bool?;
                        if (isOptional.HasValue && !isOptional.Value)
                        {
                            // Only serialize if parameter is explicitly required (Optional=false)
                            settings.Required = true;
                            hasSettings = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ParameterMapper] Error extracting Optional property from '{param.Name}': {ex.Message}");
                }
            }

            // Extract expression generically if parameter exposes an 'Expression' property
            try
            {
                var expressionProp = param.GetType().GetProperty("Expression");
                if (expressionProp != null && expressionProp.CanRead)
                {
                    var expressionObj = expressionProp.GetValue(param);
                    var expressionStr = expressionObj?.ToString();
                    if (!string.IsNullOrWhiteSpace(expressionStr))
                    {
                        settings.Expression = expressionStr;
                        hasSettings = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParameterMapper] Error extracting expression from '{param.Name}': {ex.Message}");
            }

            return hasSettings ? settings : null;
        }

        /// <summary>
        /// Checks if a parameter has custom settings that need to be serialized.
        /// </summary>
        /// <param name="param">The parameter to check.</param>
        /// <param name="isPrincipal">Whether this parameter is the principal/master input parameter.</param>
        /// <returns>True if the parameter has custom settings.</returns>
        public static bool HasCustomSettings(IGH_Param param, bool isPrincipal = false)
        {
            if (param == null)
                return false;

            // Check NickName
            if (!string.IsNullOrEmpty(param.NickName) &&
                !string.Equals(param.Name, param.NickName, StringComparison.Ordinal))
                return true;

            // Check principal
            if (isPrincipal && param.Kind == GH_ParamKind.input)
                return true;

            // Check DataMapping
            if (param.DataMapping != GH_DataMapping.None)
                return true;

            // Check modifiers
            if (param.Reverse || param.Simplify)
                return true;

            return false;
        }

        /// <summary>
        /// Applies parameter settings to a Grasshopper parameter.
        /// </summary>
        /// <param name="param">The parameter to apply settings to.</param>
        /// <param name="settings">The settings to apply.</param>
        public static void ApplySettings(IGH_Param param, ParameterSettings settings)
        {
            if (param == null || settings == null)
                return;

            // Apply NickName
            if (!string.IsNullOrEmpty(settings.NickName))
            {
                param.NickName = settings.NickName;
            }

            // Apply DataMapping
            if (!string.IsNullOrEmpty(settings.DataMapping))
            {
                if (Enum.TryParse<GH_DataMapping>(settings.DataMapping, true, out var mapping))
                {
                    param.DataMapping = mapping;
                }
            }

            // Apply flattened parameter modifiers (per schema)
            if (settings.Reverse == true)
            {
                param.Reverse = true;
            }

            if (settings.Simplify == true)
            {
                param.Simplify = true;
            }

            // Apply Invert via reflection
            if (settings.Invert == true)
            {
                try
                {
                    var invertProp = param.GetType().GetProperty("Invert");
                    if (invertProp != null && invertProp.CanWrite)
                    {
                        invertProp.SetValue(param, true);
                    }
                }
                catch
                {
                    // Property doesn't exist or can't be written
                }
            }

            // Apply Unitize via reflection
            if (settings.Unitize == true)
            {
                try
                {
                    var unitizeProp = param.GetType().GetProperty("Unitize");
                    if (unitizeProp != null && unitizeProp.CanWrite)
                    {
                        unitizeProp.SetValue(param, true);
                    }
                }
                catch
                {
                    // Property doesn't exist or can't be written
                }
            }

            // Apply Expression via reflection
            if (!string.IsNullOrEmpty(settings.Expression))
            {
                try
                {
                    var expressionProp = param.GetType().GetProperty("Expression");
                    if (expressionProp != null && expressionProp.CanWrite)
                    {
                        expressionProp.SetValue(param, settings.Expression);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ParameterMapper] Error applying expression to '{param.Name}': {ex.Message}");
                }
            }
        }
    }
}
