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
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for script component parameter I/O: access mode, type hints, variable names,
    /// and the "out" standard output parameter.
    /// Runs after <see cref="IOIdentificationHandler"/> and <see cref="IOModifiersHandler"/>
    /// so it can enrich parameter settings created by those handlers.
    /// During deserialization it clears the default parameters and recreates them from settings.
    /// </summary>
    internal sealed class ScriptIOHandler : IObjectHandler
    {
        /// <summary>
        /// Runs after core IO handlers (0) but at the same level as other extension handlers (100).
        /// </summary>
        public int Priority => 100;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component comp && IsScriptComponent(comp);
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return HasScriptExtension(component);
        }

        #region Serialization

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not IGH_Component comp)
            {
                return;
            }

            // Enrich input settings with script-specific fields
            if (component.InputSettings != null)
            {
                EnrichParameterSettings(comp.Params.Input, component.InputSettings, isInput: true);
            }

            // Enrich output settings, skipping the "out" standard output parameter
            // for IScriptComponent-based components (C#, Python, IronPython).
            // VB Script keeps "out" in output settings because it doesn't use showStandardOutput.
            if (component.OutputSettings != null)
            {
                if (HasIScriptComponentInterface(comp))
                {
                    RemoveStandardOutputEntry(comp, component.OutputSettings);
                }

                EnrichParameterSettings(comp.Params.Output, component.OutputSettings, isInput: false);
            }
        }

        private static void EnrichParameterSettings(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings> settings,
            bool isInput)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            // Match settings to parameters by index (IOIdentificationHandler creates them in order)
            // Settings count may differ from parameters count if "out" was removed
            for (int i = 0; i < settings.Count; i++)
            {
                var paramSettings = settings[i];
                var param = FindParamByName(parameters, paramSettings.ParameterName);
                if (param == null)
                {
                    continue;
                }

                // Access mode (only for inputs; output access is implicit)
                if (isInput)
                {
                    var accessStr = AccessToString(param.Access);
                    if (!string.IsNullOrEmpty(accessStr) && accessStr != "item")
                    {
                        paramSettings.Access = accessStr;
                    }
                }

                // Variable name: script params use NickName as variable name
                // Only serialize when it differs from parameterName
                var variableName = ExtractVariableName(param);
                if (!string.IsNullOrEmpty(variableName) &&
                    !string.Equals(variableName, paramSettings.ParameterName, StringComparison.Ordinal))
                {
                    paramSettings.VariableName = variableName;
                }

                // Type hint: inferred from parameter runtime type
                var typeHint = InferTypeHint(param);
                if (!string.IsNullOrEmpty(typeHint) &&
                    !string.Equals(typeHint, "object", StringComparison.OrdinalIgnoreCase))
                {
                    paramSettings.TypeHint = typeHint;
                }
            }
        }

        /// <summary>
        /// Removes the "out" standard output parameter entry from the output settings list.
        /// The "out" parameter is controlled by <c>showStandardOutput</c> in the script extension,
        /// not by the parameter settings.
        /// </summary>
        private static void RemoveStandardOutputEntry(
            IGH_Component comp,
            List<GhJsonParameterSettings> outputSettings)
        {
            if (outputSettings == null)
            {
                return;
            }

            // Find and check the actual "out" parameter on the component
            var outParam = comp.Params.Output
                .FirstOrDefault(p =>
                    p is Param_String &&
                    (p.Name.Equals("out", StringComparison.OrdinalIgnoreCase) ||
                     p.NickName.Equals("out", StringComparison.OrdinalIgnoreCase)));

            if (outParam == null)
            {
                return;
            }

            // Remove the matching entry from settings
            var entry = outputSettings.FirstOrDefault(s =>
                s.ParameterName.Equals("out", StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                outputSettings.Remove(entry);
#if DEBUG
                Debug.WriteLine("[ScriptIOHandler] Removed 'out' standard output parameter from output settings");
#endif
            }
        }

        #endregion

        #region Deserialization

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component comp)
            {
                return;
            }

            if (comp is not IGH_VariableParameterComponent varParamComp)
            {
#if DEBUG
                Debug.WriteLine("[ScriptIOHandler] Component does not implement IGH_VariableParameterComponent, skipping parameter recreation");
#endif
                return;
            }

            // Recreate input parameters from settings
            if (component.InputSettings != null && component.InputSettings.Count > 0)
            {
                RecreateParameters(comp, varParamComp, component.InputSettings, GH_ParameterSide.Input);
            }

            // Recreate output parameters from settings
            if (component.OutputSettings != null && component.OutputSettings.Count > 0)
            {
                RecreateParameters(comp, varParamComp, component.OutputSettings, GH_ParameterSide.Output);
            }

            varParamComp.VariableParameterMaintenance();
        }

        private static void RecreateParameters(
            IGH_Component comp,
            IGH_VariableParameterComponent varParamComp,
            List<GhJsonParameterSettings> settings,
            GH_ParameterSide side)
        {
            var paramList = side == GH_ParameterSide.Input ? comp.Params.Input : comp.Params.Output;
            var sideLabel = side == GH_ParameterSide.Input ? "input" : "output";

            // Capture the script variable param type from an existing user-defined parameter
            // BEFORE clearing. This avoids the Param_GenericObject fallback that causes
            // InvalidCastException when the component expects ScriptVariableParam.
            var scriptParamType = CaptureScriptParamType(comp);

#if DEBUG
            Debug.WriteLine($"[ScriptIOHandler] Recreating {settings.Count} {sideLabel} parameters (clearing {paramList.Count} defaults, captured type: {scriptParamType?.Name ?? "null"})");
#endif

            // Clear existing parameters
            paramList.Clear();

            for (int i = 0; i < settings.Count; i++)
            {
                var s = settings[i];

                // Create a parameter: prefer Activator with captured type, fallback to component factory
                IGH_Param? param = CreateScriptParam(scriptParamType, varParamComp, side, i);

                if (param == null)
                {
#if DEBUG
                    Debug.WriteLine($"[ScriptIOHandler] Failed to create parameter for {sideLabel}[{i}], skipping");
#endif
                    continue;
                }

                // Set identification
                var name = s.ParameterName;
                param.Name = name;
                param.NickName = name;
                param.Description = string.Empty;

                // Apply variable name (script params store it as NickName)
                if (!string.IsNullOrEmpty(s.VariableName))
                {
                    param.NickName = s.VariableName;

                    // Also try to set VariableName property if it exists
                    TrySetProperty(param, "VariableName", s.VariableName);
                }

                // Apply access mode (only for inputs)
                if (side == GH_ParameterSide.Input && !string.IsNullOrEmpty(s.Access))
                {
                    param.Access = AccessFromString(s.Access);
                }

                // Enable tree access for script parameters
                TrySetProperty(param, "AllowTreeAccess", true);

                // Register parameter
                if (side == GH_ParameterSide.Input)
                {
                    comp.Params.RegisterInputParam(param);
                }
                else
                {
                    comp.Params.RegisterOutputParam(param);
                }

                // Apply type hint AFTER registration (some params need to be registered first)
                var registeredParam = side == GH_ParameterSide.Input
                    ? comp.Params.Input[i]
                    : comp.Params.Output[i];

                if (!string.IsNullOrEmpty(s.TypeHint))
                {
                    ApplyTypeHint(registeredParam, s.TypeHint);
                }

                // Apply NickName override if different from VariableName
                if (!string.IsNullOrEmpty(s.NickName))
                {
                    registeredParam.NickName = s.NickName;
                }

                // Apply modifiers that IOModifiersHandler would have applied to the (now deleted) old params
                ApplyModifiers(registeredParam, s);

#if DEBUG
                Debug.WriteLine($"[ScriptIOHandler] Registered {sideLabel} parameter '{name}' (access={s.Access}, typeHint={s.TypeHint})");
#endif
            }
        }

        /// <summary>
        /// Applies all modifier settings to a parameter.
        /// This is necessary because <see cref="IOModifiersHandler"/> ran on the default
        /// parameters which have been cleared and replaced.
        /// </summary>
        private static void ApplyModifiers(IGH_Param param, GhJsonParameterSettings settings)
        {
            // Data mapping
            if (!string.IsNullOrEmpty(settings.DataMapping) &&
                Enum.TryParse<GH_DataMapping>(settings.DataMapping, out var mapping))
            {
                param.DataMapping = mapping;
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
                var prop = ReflectionCache.GetProperty(param.GetType(), "Reparameterize");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(param, true);
                }
            }

            // Invert (Param_Boolean only)
            if (settings.IsInverted == true)
            {
                var prop = ReflectionCache.GetProperty(param.GetType(), "Invert");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(param, true);
                }
            }

            // Unitize (Param_Vector only)
            if (settings.IsUnitized == true)
            {
                var prop = ReflectionCache.GetProperty(param.GetType(), "Unitize");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(param, true);
                }
            }

            // IsPrincipal
            if (settings.IsPrincipal == true)
            {
                var method = ReflectionCache.GetMethod(param.GetType(), "SetPrincipal");
                if (method != null)
                {
                    method.Invoke(param, new object[] { true, false, false });
                }
            }

            // Expression
            if (!string.IsNullOrEmpty(settings.Expression))
            {
                var prop = ReflectionCache.GetProperty(param.GetType(), "Expression");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(param, settings.Expression);
                }
            }
        }

        #endregion

        #region Parameter Creation Helpers

        /// <summary>
        /// Captures the runtime type of a user-defined script variable parameter from the component.
        /// Looks at inputs first (always have default x, y), then non-"out" outputs.
        /// Must be called BEFORE clearing parameter lists.
        /// </summary>
        private static Type? CaptureScriptParamType(IGH_Component comp)
        {
            // Prefer input params
            foreach (var p in comp.Params.Input)
            {
                // Skip standard Grasshopper parameter types — we want the script-specific type
                var typeName = p.GetType().Name;
                if (typeName != "Param_GenericObject" && typeName != "Param_String")
                {
                    return p.GetType();
                }
            }

            // Fallback: check output params, skip "out" (Param_String)
            foreach (var p in comp.Params.Output)
            {
                if (p is not Param_String)
                {
                    return p.GetType();
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a script parameter instance using the captured type or the component's factory.
        /// </summary>
        private static IGH_Param? CreateScriptParam(
            Type? scriptParamType,
            IGH_VariableParameterComponent varParamComp,
            GH_ParameterSide side,
            int index)
        {
            // Strategy 1: create from captured type via parameterless constructor
            if (scriptParamType != null)
            {
                try
                {
                    var instance = Activator.CreateInstance(scriptParamType) as IGH_Param;
                    if (instance != null)
                    {
                        return instance;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine($"[ScriptIOHandler] Activator.CreateInstance({scriptParamType.Name}) failed: {ex.Message}");
#endif
                }
            }

            // Strategy 2: use the component's factory (works for inputs, may fail for output[0])
            if (varParamComp.CanInsertParameter(side, index))
            {
                var param = varParamComp.CreateParameter(side, index);
                if (param != null)
                {
                    return param;
                }
            }

            // Strategy 3: try factory at a different index (output[0] is reserved for "out")
            if (side == GH_ParameterSide.Output && varParamComp.CanInsertParameter(side, index + 1))
            {
                var param = varParamComp.CreateParameter(side, index + 1);
                if (param != null)
                {
                    return param;
                }
            }

#if DEBUG
            Debug.WriteLine($"[ScriptIOHandler] All creation strategies failed for {side}[{index}]");
#endif
            return null;
        }

        #endregion

        #region Type Hint Helpers

        /// <summary>
        /// Infers the type hint from a script parameter's runtime type information.
        /// </summary>
        private static string? InferTypeHint(IGH_Param param)
        {
            try
            {
                // Try IScriptParameter.Converter → TargetType → Type.Name
                var converterField = param.GetType().GetField(
                    "_converter",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (converterField != null)
                {
                    var converter = converterField.GetValue(param);
                    if (converter != null)
                    {
                        var targetTypeProp = ReflectionCache.GetProperty(converter.GetType(), "TargetType");
                        if (targetTypeProp != null)
                        {
                            var targetType = targetTypeProp.GetValue(converter);
                            if (targetType != null)
                            {
                                var typeProp = ReflectionCache.GetProperty(targetType.GetType(), "Type");
                                if (typeProp != null)
                                {
                                    var type = typeProp.GetValue(targetType) as Type;
                                    if (type != null)
                                    {
                                        return type.Name;
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback: try TypeHint string property
                var typeHintProp = ReflectionCache.GetProperty(param.GetType(), "TypeHint");
                if (typeHintProp != null && typeHintProp.CanRead)
                {
                    var hint = typeHintProp.GetValue(param) as string;
                    if (!string.IsNullOrEmpty(hint))
                    {
                        return hint;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ScriptIOHandler] Error inferring type hint for '{param.Name}': {ex.Message}");
#endif
            }

            return null;
        }

        /// <summary>
        /// Applies a type hint to a registered script parameter using <c>TypeHints.Select(name)</c>.
        /// </summary>
        private static void ApplyTypeHint(IGH_Param param, string typeHint)
        {
            if (string.IsNullOrEmpty(typeHint) ||
                string.Equals(typeHint, "object", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                // Strategy 1: TypeHints.Select(string) — works for ScriptVariableParam
                var typeHintsProp = ReflectionCache.GetProperty(param.GetType(), "TypeHints");
                if (typeHintsProp != null && typeHintsProp.CanRead)
                {
                    var typeHints = typeHintsProp.GetValue(param);
                    if (typeHints != null)
                    {
                        var selectMethod = typeHints.GetType().GetMethod("Select", new[] { typeof(string) });
                        if (selectMethod != null)
                        {
                            selectMethod.Invoke(typeHints, new object[] { typeHint });
#if DEBUG
                            Debug.WriteLine($"[ScriptIOHandler] Applied type hint '{typeHint}' to '{param.Name}' via TypeHints.Select()");
#endif
                            return;
                        }
                    }
                }

                // Strategy 2: direct TypeHint property (string)
                var typeHintProp = ReflectionCache.GetProperty(param.GetType(), "TypeHint");
                if (typeHintProp != null && typeHintProp.CanWrite && typeHintProp.PropertyType == typeof(string))
                {
                    typeHintProp.SetValue(param, typeHint);
#if DEBUG
                    Debug.WriteLine($"[ScriptIOHandler] Applied type hint '{typeHint}' to '{param.Name}' via TypeHint property");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ScriptIOHandler] Error applying type hint '{typeHint}' to '{param.Name}': {ex.Message}");
#endif
            }
        }

        #endregion

        #region Utility Helpers

        /// <summary>
        /// Extracts the variable name from a script parameter via reflection.
        /// Falls back to NickName.
        /// </summary>
        private static string? ExtractVariableName(IGH_Param param)
        {
            // Try VariableName property first
            var vnProp = ReflectionCache.GetProperty(param.GetType(), "VariableName");
            if (vnProp != null && vnProp.CanRead)
            {
                var vn = vnProp.GetValue(param) as string;
                if (!string.IsNullOrEmpty(vn))
                {
                    return vn;
                }
            }

            // Fall back to NickName (script params use NickName as variable name)
            return param.NickName;
        }

        /// <summary>
        /// Known script component GUIDs (C#, Python 3, IronPython 2, VB.NET).
        /// </summary>
        private static readonly HashSet<Guid> ScriptComponentGuids = new()
        {
            new Guid("b6ba1144-02d6-4a2d-b53c-ec62e290eeb7"), // C# Script
            new Guid("719467e6-7cf5-4848-99b0-c5dd57e5442c"), // Python 3 Script
            new Guid("97aa26ef-88ae-4ba6-98a6-ed6ddeca11d1"), // IronPython 2 Script
            new Guid("079bd9bd-54a0-41d4-98af-db999015f63d"), // VB Script
        };

        private static bool IsScriptComponent(IGH_Component comp)
        {
            // Check IScriptComponent interface (C#, Python, IronPython in Rhino 8+)
            if (HasIScriptComponentInterface(comp))
            {
                return true;
            }

            // Check old GhPython component (ZuiPythonComponent from GhPython.dll)
            // This covers Ladybug Tools, Honeybee, Dragonfly, and the generic GhPython Script.
            if (IsGhPythonComponent(comp))
            {
                return true;
            }

            // Fallback: match by GUID for legacy components (VB.NET)
            return comp is IGH_VariableParameterComponent &&
                   ScriptComponentGuids.Contains(comp.ComponentGuid);
        }

        private static bool HasIScriptComponentInterface(IGH_Component comp)
        {
            return comp.GetType().GetInterfaces().Any(i => i.Name == "IScriptComponent");
        }

        /// <summary>
        /// Checks if the component is an old GhPython component (<c>ZuiPythonComponent</c> from GhPython.dll).
        /// This covers Ladybug Tools, Honeybee, Dragonfly, and the generic GhPython Script component.
        /// </summary>
        private static bool IsGhPythonComponent(IGH_Component comp)
        {
            return comp.GetType().Name == "ZuiPythonComponent";
        }

        private static bool HasScriptExtension(GhJsonComponent component)
        {
            if (component.ComponentState?.Extensions == null)
            {
                return false;
            }

            return component.ComponentState.Extensions.ContainsKey("gh.csharp") ||
                   component.ComponentState.Extensions.ContainsKey("gh.python") ||
                   component.ComponentState.Extensions.ContainsKey("gh.ironpython") ||
                   component.ComponentState.Extensions.ContainsKey("gh.vbscript") ||
                   component.ComponentState.Extensions.ContainsKey("gh.ghpython");
        }

        private static IGH_Param? FindParamByName(IList<IGH_Param> parameters, string name)
        {
            foreach (var p in parameters)
            {
                if (string.Equals(p.Name, name, StringComparison.Ordinal))
                {
                    return p;
                }
            }

            return null;
        }

        private static string AccessToString(GH_ParamAccess access)
        {
            return access switch
            {
                GH_ParamAccess.item => "item",
                GH_ParamAccess.list => "list",
                GH_ParamAccess.tree => "tree",
                _ => "item",
            };
        }

        private static GH_ParamAccess AccessFromString(string? access)
        {
            return access?.ToLowerInvariant() switch
            {
                "list" => GH_ParamAccess.list,
                "tree" => GH_ParamAccess.tree,
                _ => GH_ParamAccess.item,
            };
        }

        private static void TrySetProperty(object target, string propertyName, object value)
        {
            try
            {
                var prop = ReflectionCache.GetProperty(target.GetType(), propertyName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value);
                }
            }
            catch
            {
                // Silently ignore — property may not exist on all parameter types
            }
        }

        #endregion
    }
}
