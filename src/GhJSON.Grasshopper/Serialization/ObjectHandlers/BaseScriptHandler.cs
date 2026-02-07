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
    internal abstract class BaseScriptHandler : IObjectHandler, IPostPlacementHandler
    {
        public int Priority => 100;

        public string? SchemaExtensionUrl => null;

        public abstract string ExtensionKey { get; }

        protected abstract Guid ComponentGuid { get; }

        protected abstract string ComponentName { get; }

        public virtual bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component comp && comp.ComponentGuid == this.ComponentGuid;
        }

        public virtual bool CanHandle(GhJsonComponent component)
        {
            return component.Name == this.ComponentName ||
                   component.ComponentGuid == this.ComponentGuid;
        }

        public virtual void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not IGH_Component scriptComp)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var code = ExtractScriptCode(scriptComp);
            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            var data = new Dictionary<string, object>
            {
                ["code"] = code,
            };

            if (TryReadUsingStandardOutputParam(scriptComp, out var showStdOutput))
            {
                data["showStandardOutput"] = showStdOutput;
            }

            // Capture "out" parameter modifiers from the live component.
            // ScriptIOHandler removes "out" from outputSettings, so we preserve modifiers here.
            SerializeOutParamModifiers(scriptComp, data);

            // Capture IScriptComponent marshalling options
            SerializeMarshOptions(scriptComp, data);

            component.ComponentState.Extensions[this.ExtensionKey] = data;
        }

        public virtual void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component scriptComp ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(this.ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> data)
            {
                return;
            }

            if (data.TryGetValue("code", out var codeObj))
            {
                var code = codeObj?.ToString();
                if (!string.IsNullOrEmpty(code))
                {
                    ApplyScriptCode(scriptComp, code);
                }
            }

            if (data.TryGetValue("showStandardOutput", out var showObj) && bool.TryParse(showObj?.ToString(), out var desired))
            {
                TryApplyUsingStandardOutputParam(scriptComp, desired);
            }

            // Restore "out" parameter modifiers after the param has been (re-)created
            DeserializeOutParamModifiers(scriptComp, data);

            // NOTE: Marshalling options are NOT applied here because AddedToDocument resets them.
            // They are applied in PostPlacement() after the component is added to the document.
        }

        private static string? ExtractScriptCode(IGH_Component component)
        {
            try
            {
                try
                {
                    var typeOfScript = component.GetType();
                    var scriptInterface = typeOfScript.GetInterfaces().FirstOrDefault(i => i.Name == "IScriptComponent");
                    if (scriptInterface != null)
                    {
                        var textProp = ReflectionCache.GetProperty(scriptInterface, "Text");
                        if (textProp != null && textProp.CanRead)
                        {
                            var value = textProp.GetValue(component)?.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
                catch
                {
                }

                var type = component.GetType();
                string[] candidates = { "Text", "Script", "Code", "ScriptCode", "Source", "SourceCode" };

                foreach (var name in candidates)
                {
                    var prop = ReflectionCache.GetProperty(type, name);
                    if (prop != null && prop.CanRead)
                    {
                        var value = prop.GetValue(component)?.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }

                var scriptSourceProp = ReflectionCache.GetProperty(type, "ScriptSource");
                if (scriptSourceProp != null && scriptSourceProp.CanRead)
                {
                    var scriptSourceObj = scriptSourceProp.GetValue(component);
                    if (scriptSourceObj != null)
                    {
                        var scriptSourceType = scriptSourceObj.GetType();
                        string[] sourceCandidates = { "ScriptCode", "Code", "Text", "Source", "SourceCode" };

                        foreach (var name in sourceCandidates)
                        {
                            var prop = ReflectionCache.GetProperty(scriptSourceType, name);
                            if (prop != null && prop.CanRead)
                            {
                                var value = prop.GetValue(scriptSourceObj)?.ToString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[BaseScriptHandler] Error extracting script code: {ex.Message}");
#endif
            }

            return null;
        }

        private static void ApplyScriptCode(IGH_Component component, string scriptCode)
        {
            try
            {
                var type = component.GetType();

                try
                {
                    var scriptInterface = type.GetInterfaces().FirstOrDefault(i => i.Name == "IScriptComponent");
                    if (scriptInterface != null)
                    {
                        var textProp = ReflectionCache.GetProperty(scriptInterface, "Text");
                        if (textProp != null && textProp.CanWrite)
                        {
                            textProp.SetValue(component, scriptCode);
                            return;
                        }
                    }
                }
                catch
                {
                }

                string[] candidates = { "Text", "Script", "Code", "ScriptCode", "Source", "SourceCode" };
                foreach (var name in candidates)
                {
                    var prop = ReflectionCache.GetProperty(type, name);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(component, scriptCode);
                        return;
                    }
                }

                var scriptSourceProp = ReflectionCache.GetProperty(type, "ScriptSource");
                if (scriptSourceProp != null && scriptSourceProp.CanRead)
                {
                    var scriptSourceObj = scriptSourceProp.GetValue(component);
                    if (scriptSourceObj != null)
                    {
                        var scriptSourceType = scriptSourceObj.GetType();
                        string[] sourceCandidates = { "ScriptCode", "Code", "Text", "Source", "SourceCode" };
                        foreach (var name in sourceCandidates)
                        {
                            var prop = ReflectionCache.GetProperty(scriptSourceType, name);
                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(scriptSourceObj, scriptCode);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[BaseScriptHandler] Error applying script code: {ex.Message}");
#endif
            }
        }

        #region Out Parameter Modifiers

        /// <summary>
        /// Captures modifiers (Simplify, Reverse, DataMapping, Expression) from the live "out" parameter
        /// and stores them in the extension data dictionary.
        /// </summary>
        private static void SerializeOutParamModifiers(IGH_Component comp, Dictionary<string, object> data)
        {
            var outParam = FindOutParam(comp);
            if (outParam == null)
            {
                return;
            }

            var modifiers = new Dictionary<string, object>();

            if (outParam.Simplify)
            {
                modifiers["isSimplified"] = true;
            }

            if (outParam.Reverse)
            {
                modifiers["isReversed"] = true;
            }

            if (outParam.DataMapping != GH_DataMapping.None)
            {
                modifiers["dataMapping"] = outParam.DataMapping.ToString();
            }

            var exprProp = ReflectionCache.GetProperty(outParam.GetType(), "Expression");
            if (exprProp != null && exprProp.CanRead)
            {
                var expr = exprProp.GetValue(outParam) as string;
                if (!string.IsNullOrEmpty(expr))
                {
                    modifiers["expression"] = expr;
                }
            }

            if (modifiers.Count > 0)
            {
                data["outModifiers"] = modifiers;
#if DEBUG
                Debug.WriteLine($"[BaseScriptHandler] Captured {modifiers.Count} 'out' param modifiers");
#endif
            }
        }

        /// <summary>
        /// Applies stored modifiers to the "out" parameter after it has been (re-)created
        /// by <see cref="TryApplyUsingStandardOutputParam"/>.
        /// </summary>
        private static void DeserializeOutParamModifiers(IGH_Component comp, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("outModifiers", out var modObj) ||
                modObj is not Dictionary<string, object> modifiers)
            {
                return;
            }

            var outParam = FindOutParam(comp);
            if (outParam == null)
            {
#if DEBUG
                Debug.WriteLine("[BaseScriptHandler] Cannot apply 'out' modifiers: param not found");
#endif
                return;
            }

            if (modifiers.TryGetValue("isSimplified", out var simpObj) &&
                bool.TryParse(simpObj?.ToString(), out var simp) && simp)
            {
                outParam.Simplify = true;
            }

            if (modifiers.TryGetValue("isReversed", out var revObj) &&
                bool.TryParse(revObj?.ToString(), out var rev) && rev)
            {
                outParam.Reverse = true;
            }

            if (modifiers.TryGetValue("dataMapping", out var dmObj) &&
                Enum.TryParse<GH_DataMapping>(dmObj?.ToString(), out var dm))
            {
                outParam.DataMapping = dm;
            }

            if (modifiers.TryGetValue("expression", out var exprObj))
            {
                var expr = exprObj?.ToString();
                if (!string.IsNullOrEmpty(expr))
                {
                    var exprProp = ReflectionCache.GetProperty(outParam.GetType(), "Expression");
                    if (exprProp != null && exprProp.CanWrite)
                    {
                        exprProp.SetValue(outParam, expr);
                    }
                }
            }

#if DEBUG
            Debug.WriteLine($"[BaseScriptHandler] Applied 'out' param modifiers");
#endif
        }

        /// <summary>
        /// Finds the "out" standard output parameter on the component.
        /// </summary>
        private static IGH_Param? FindOutParam(IGH_Component comp)
        {
            return comp.Params.Output.FirstOrDefault(p =>
                p.Name.Equals("out", StringComparison.OrdinalIgnoreCase) ||
                p.NickName.Equals("out", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Post-Placement (IPostPlacementHandler)

        /// <inheritdoc/>
        public virtual void PostPlacement(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component scriptComp ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(this.ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> data)
            {
                return;
            }

            // Apply marshalling options now that the component is in the document
            // and AddedToDocument / Context.InitLanguages has already run.
            DeserializeMarshOptions(scriptComp, data);
        }

        #endregion

        #region Marshalling Options

        /// <summary>
        /// Property names on the <c>IScriptComponent</c> interface for marshalling options.
        /// </summary>
        private static readonly string[] MarshPropertyNames = { "MarshGuids", "MarshOutputs", "MarshInputs" };

        /// <summary>
        /// Maps <c>IScriptComponent</c> property names to their GhJSON extension key names.
        /// <c>MarshGuids=true</c> on the API means "Avoid Marshalling Output Guids" is enabled,
        /// so we use descriptive key names that match the Grasshopper UI terminology.
        /// </summary>
        private static readonly Dictionary<string, string> MarshToJsonKey = new()
        {
            ["MarshGuids"] = "avoidMarshalGuids",
            ["MarshOutputs"] = "avoidGraftOutputs",
            ["MarshInputs"] = "avoidMarshalInputs",
        };

        /// <summary>
        /// Serializes <c>IScriptComponent</c> marshalling options (MarshGuids, MarshOutputs, MarshInputs)
        /// into the extension data dictionary. Only non-default (<c>true</c>) values are stored.
        /// On <c>IScriptComponent</c>, <c>true</c> means the "Avoid" option is enabled (non-default).
        /// </summary>
        private static void SerializeMarshOptions(IGH_Component comp, Dictionary<string, object> data)
        {
            var scriptInterface = comp.GetType().GetInterfaces()
                .FirstOrDefault(i => i.Name == "IScriptComponent");
            if (scriptInterface == null)
            {
                return;
            }

            foreach (var propName in MarshPropertyNames)
            {
                var prop = ReflectionCache.GetProperty(scriptInterface, propName);
                if (prop != null && prop.CanRead)
                {
                    var value = prop.GetValue(comp);
                    if (value is bool b && b)
                    {
                        // Store only when true (= "avoid" is enabled), false is the default
                        var jsonKey = MarshToJsonKey[propName];
                        data[jsonKey] = true;
#if DEBUG
                        Debug.WriteLine($"[BaseScriptHandler] Serialized {jsonKey}=true (from {propName}=true)");
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes <c>IScriptComponent</c> marshalling options from the extension data dictionary.
        /// Supports both current keys (<c>avoidMarshalGuids</c>, etc.) and legacy keys (<c>marshGuids</c>, etc.).
        /// Uses the concrete type's interface map to invoke the actual setter, which is more
        /// reliable than calling <see cref="PropertyInfo.SetValue"/> on an interface property.
        /// </summary>
        private static void DeserializeMarshOptions(IGH_Component comp, Dictionary<string, object> data)
        {
            var compType = comp.GetType();
            var scriptInterface = compType.GetInterfaces()
                .FirstOrDefault(i => i.Name == "IScriptComponent");
            if (scriptInterface == null)
            {
                return;
            }

            foreach (var propName in MarshPropertyNames)
            {
                // Try current key first, then fall back to legacy key
                var jsonKey = MarshToJsonKey[propName];
                bool val;

                if (data.TryGetValue(jsonKey, out var valObj) &&
                    bool.TryParse(valObj?.ToString(), out val))
                {
                    // Current format: value maps directly to the IScriptComponent property
                }
                else
                {
                    continue;
                }

                try
                {
                    // Use the interface map to resolve the concrete setter method.
                    // This is more reliable than PropertyInfo.SetValue on an interface property,
                    // which can silently fail for explicit interface implementations.
                    var interfaceProp = scriptInterface.GetProperty(propName);
                    var interfaceSetter = interfaceProp?.GetSetMethod();
                    if (interfaceSetter == null)
                    {
#if DEBUG
                        Debug.WriteLine($"[BaseScriptHandler] No setter found on interface for {propName}");
#endif
                        continue;
                    }

                    var map = compType.GetInterfaceMap(scriptInterface);
                    MethodInfo? concreteSetter = null;
                    for (int i = 0; i < map.InterfaceMethods.Length; i++)
                    {
                        if (map.InterfaceMethods[i] == interfaceSetter)
                        {
                            concreteSetter = map.TargetMethods[i];
                            break;
                        }
                    }

                    if (concreteSetter != null)
                    {
                        concreteSetter.Invoke(comp, new object[] { val });
                    }
                    else
                    {
                        // Fallback: try setting via interface PropertyInfo
                        interfaceProp.SetValue(comp, val);
                    }

#if DEBUG
                    // Read-back verification
                    var getter = interfaceProp.GetGetMethod();
                    MethodInfo? concreteGetter = null;
                    if (getter != null)
                    {
                        for (int i = 0; i < map.InterfaceMethods.Length; i++)
                        {
                            if (map.InterfaceMethods[i] == getter)
                            {
                                concreteGetter = map.TargetMethods[i];
                                break;
                            }
                        }
                    }

                    var readBack = concreteGetter?.Invoke(comp, null);
                    Debug.WriteLine($"[BaseScriptHandler] Restored {propName}={val}, readBack={readBack}");
#endif
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine($"[BaseScriptHandler] Error restoring {propName}: {ex.Message}");
#endif
                }
            }
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        #endregion

        #region Standard Output Parameter

        protected static bool TryReadUsingStandardOutputParam(IGH_Component component, out bool value)
        {
            value = false;

            try
            {
                var usingStdOutputProp = ReflectionCache.GetProperty(component.GetType(), "UsingStandardOutputParam");
                if (usingStdOutputProp != null && usingStdOutputProp.CanRead)
                {
                    var obj = usingStdOutputProp.GetValue(component);
                    if (obj is bool b)
                    {
                        value = b;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        protected static void TryApplyUsingStandardOutputParam(IGH_Component component, bool desired)
        {
            try
            {
                var usingStdOutputProp = ReflectionCache.GetProperty(component.GetType(), "UsingStandardOutputParam");
                if (usingStdOutputProp == null || !usingStdOutputProp.CanWrite)
                {
                    return;
                }

                bool current = (bool)usingStdOutputProp.GetValue(component);

                if (current == desired)
                {
                    usingStdOutputProp.SetValue(component, !desired);
                    if (component is IGH_VariableParameterComponent varParamComp)
                    {
                        varParamComp.VariableParameterMaintenance();
                    }
                }

                usingStdOutputProp.SetValue(component, desired);
                if (component is IGH_VariableParameterComponent varParamComp2)
                {
                    varParamComp2.VariableParameterMaintenance();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[BaseScriptHandler] Error applying UsingStandardOutputParam: {ex.Message}");
#endif
            }
        }

        #endregion
    }
}
