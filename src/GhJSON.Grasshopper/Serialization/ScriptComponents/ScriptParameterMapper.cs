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
using System.Reflection;
using GhJSON.Core.Models.Components;
using GhJSON.Grasshopper.Serialization.Shared;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;

namespace GhJSON.Grasshopper.Serialization.ScriptComponents
{
    /// <summary>
    /// Handles bidirectional mapping for script component parameters.
    /// Manages variable names, type hints, and access modes for script components.
    /// </summary>
    public static class ScriptParameterMapper
    {
        public static IGH_Param? CreateParameter(
            ParameterSettings settings,
            string defaultName,
            ScriptLanguage language,
            bool isOutput)
        {
            if (settings == null)
                return null;

            var variableNameRaw = settings.VariableName ?? settings.ParameterName ?? defaultName;
            var variableNameCtor = language == ScriptLanguage.CSharp
                ? SanitizeCSharpIdentifier(variableNameRaw)
                : variableNameRaw;

            IGH_Param? param = null;

            if (language == ScriptLanguage.VB)
            {
                param = new Param_ScriptVariable
                {
                    Name = variableNameRaw,
                    NickName = variableNameRaw,
                    Description = string.Empty,
                    Access = AccessModeMapper.FromString(settings.Access),
                };
            }
            else
            {
                var scriptParamType = typeof(GH_Panel).Assembly.GetType("Grasshopper.Kernel.Special.ScriptVariableParam", throwOnError: false);
                if (scriptParamType != null)
                {
                    param = Activator.CreateInstance(scriptParamType, new object[] { variableNameCtor }) as IGH_Param;
                }

                param ??= new Param_GenericObject();
                param.Name = variableNameRaw;
                param.NickName = variableNameRaw;
                param.Description = string.Empty;
                param.Access = AccessModeMapper.FromString(settings.Access);

                TrySetProperty(param, "VariableName", variableNameCtor);
                TrySetProperty(param, "AllowTreeAccess", true);
            }

            TryApplyOptional(param, settings);

            ApplySettings(param, settings);

            if (!isOutput && settings.IsPrincipal == true)
            {
                // Consumer sets MasterParameterIndex.
            }

            return param;
        }

        /// <summary>
        /// Extracts parameter settings from a script component parameter.
        /// </summary>
        /// <param name="param">Script parameter to extract from.</param>
        /// <param name="isPrincipal">Whether this is a principal parameter.</param>
        /// <returns>ParameterSettings with script-specific data.</returns>
        public static ParameterSettings? ExtractSettings(IGH_Param? param, bool isPrincipal = false)
        {
            if (param == null)
                return null;

            var settings = new ParameterSettings
            {
                ParameterName = param.Name,
                Access = param.Kind == GH_ParamKind.input
                    ? AccessModeMapper.ToString(param.Access)
                    : null,
            };

            bool hasSettings = true;

            // Extract variable name from NickName (script parameters use NickName as variable name)
            var variableName = param.NickName;
            if (!string.IsNullOrEmpty(variableName) &&
                !string.Equals(settings.ParameterName, variableName, StringComparison.Ordinal))
            {
                settings.VariableName = variableName;
            }

            // Mark as principal if applicable (only valid for inputs)
            if (isPrincipal && param.Kind == GH_ParamKind.input)
            {
                settings.IsPrincipal = true;
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
                            settings.Required = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ScriptParameterMapper] Error extracting Optional from '{param.Name}': {ex.Message}");
                }
            }

            // Extract DataMapping (Flatten, Graft)
            if (param.DataMapping != GH_DataMapping.None)
            {
                settings.DataMapping = param.DataMapping.ToString();
            }

            // Extract additional settings (Reverse, Simplify, etc.)
            var additionalSettings = ExtractAdditionalSettings(param);
            if (additionalSettings != null)
            {
                settings.AdditionalSettings = additionalSettings;
            }

            // Try to extract type hint from script parameter
            settings.TypeHint = ExtractTypeHint(param);

            return hasSettings ? settings : null;
        }

        /// <summary>
        /// Extracts type hint from a script parameter.
        /// </summary>
        /// <param name="param">The parameter to extract type hint from.</param>
        /// <returns>Type hint string or null.</returns>
        public static string? ExtractTypeHint(IGH_Param param)
        {
            if (param == null)
                return null;

            try
            {
                // Try to get TypeHint property via reflection
                var typeHintProp = param.GetType().GetProperty("TypeHint");
                if (typeHintProp != null && typeHintProp.CanRead)
                {
                    var typeHintObj = typeHintProp.GetValue(param);
                    if (typeHintObj != null)
                    {
                        // Get the TypeName property from the type hint object
                        var typeNameProp = typeHintObj.GetType().GetProperty("TypeName");
                        if (typeNameProp != null && typeNameProp.CanRead)
                        {
                            var typeName = typeNameProp.GetValue(typeHintObj)?.ToString();
                            if (!string.IsNullOrWhiteSpace(typeName))
                            {
                                return TypeHintMapper.FormatTypeHint(typeName, param.Access);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScriptParameterMapper] Error extracting type hint from '{param.Name}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Extracts additional parameter settings (modifiers) from a parameter.
        /// </summary>
        private static AdditionalParameterSettings? ExtractAdditionalSettings(IGH_Param param)
        {
            var additionalSettings = new AdditionalParameterSettings();
            bool hasAdditionalSettings = false;

            // Extract Reverse flag
            if (param.Reverse)
            {
                additionalSettings.Reverse = true;
                hasAdditionalSettings = true;
            }

            // Extract Simplify flag
            if (param.Simplify)
            {
                additionalSettings.Simplify = true;
                hasAdditionalSettings = true;
            }

            // Extract Invert flag via reflection
            try
            {
                var invertProp = param.GetType().GetProperty("Invert");
                if (invertProp != null && invertProp.CanRead)
                {
                    var invertValue = invertProp.GetValue(param) as bool?;
                    if (invertValue == true)
                    {
                        additionalSettings.Invert = true;
                        hasAdditionalSettings = true;
                    }
                }
            }
            catch
            {
                // Property doesn't exist or can't be read
            }

            return hasAdditionalSettings ? additionalSettings : null;
        }

        /// <summary>
        /// Applies parameter settings to a script parameter.
        /// </summary>
        /// <param name="param">The parameter to apply settings to.</param>
        /// <param name="settings">The settings to apply.</param>
        public static void ApplySettings(IGH_Param param, ParameterSettings settings)
        {
            if (param == null || settings == null)
                return;

            // Apply variable name as NickName
            if (!string.IsNullOrEmpty(settings.VariableName))
            {
                param.NickName = settings.VariableName;
            }
            else if (!string.IsNullOrEmpty(settings.NickName))
            {
                param.NickName = settings.NickName;
            }

            // Apply Access mode
            if (!string.IsNullOrEmpty(settings.Access))
            {
                param.Access = AccessModeMapper.FromString(settings.Access);
            }

            // Apply DataMapping
            if (!string.IsNullOrEmpty(settings.DataMapping))
            {
                if (Enum.TryParse<GH_DataMapping>(settings.DataMapping, true, out var mapping))
                {
                    param.DataMapping = mapping;
                }
            }

            // Apply additional settings
            if (settings.AdditionalSettings != null)
            {
                if (settings.AdditionalSettings.Reverse == true)
                {
                    param.Reverse = true;
                }

                if (settings.AdditionalSettings.Simplify == true)
                {
                    param.Simplify = true;
                }
            }

            // Apply type hint via reflection
            if (!string.IsNullOrEmpty(settings.TypeHint))
            {
                ApplyTypeHint(param, settings.TypeHint);
            }
        }

        /// <summary>
        /// Applies a type hint to a script parameter.
        /// </summary>
        private static void ApplyTypeHint(IGH_Param param, string typeHint)
        {
            try
            {
                ApplyTypeHintToParameter(param, typeHint);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScriptParameterMapper] Error applying type hint to '{param.Name}': {ex.Message}");
            }
        }

        public static void ApplyTypeHintToParameter(IGH_Param param, string typeHint)
        {
            if (param == null || string.IsNullOrWhiteSpace(typeHint))
                return;

            var baseType = TypeHintMapper.ExtractBaseType(typeHint) ?? typeHint;
            if (string.Equals(baseType, "object", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var typeHintsProp = param.GetType().GetProperty("TypeHints");
                if (typeHintsProp != null)
                {
                    var typeHintsObj = typeHintsProp.GetValue(param);
                    if (typeHintsObj != null)
                    {
                        var selectMethod = typeHintsObj.GetType().GetMethod("Select", new[] { typeof(string) });
                        if (selectMethod != null)
                        {
                            selectMethod.Invoke(typeHintsObj, new object[] { baseType });
                            return;
                        }
                    }
                }

                var typeHintProp = param.GetType().GetProperty("TypeHint");
                if (typeHintProp != null && typeHintProp.CanWrite && typeHintProp.PropertyType == typeof(string))
                {
                    typeHintProp.SetValue(param, baseType);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScriptParameterMapper] Error applying type hint '{baseType}' to '{param.Name}': {ex.Message}");
            }
        }

        private static void TryApplyOptional(IGH_Param param, ParameterSettings settings)
        {
            try
            {
                var optionalProp = param.GetType().GetProperty("Optional");
                if (optionalProp != null && optionalProp.CanWrite)
                {
                    bool isOptional = settings.Required.HasValue ? !settings.Required.Value : true;
                    optionalProp.SetValue(param, isOptional);
                }
            }
            catch
            {
            }
        }

        private static void TrySetProperty(object obj, string propertyName, object value)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(obj, value);
                }
            }
            catch
            {
            }
        }

        private static string SanitizeCSharpIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_";

            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                bool ok = c == '_' || char.IsLetterOrDigit(c);
                chars[i] = ok ? c : '_';
            }

            var result = new string(chars);
            if (!char.IsLetter(result[0]) && result[0] != '_')
            {
                result = "_" + result;
            }

            return result;
        }
    }
}
