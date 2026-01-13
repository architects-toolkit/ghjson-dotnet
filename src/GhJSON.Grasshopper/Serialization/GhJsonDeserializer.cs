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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.ComponentHandlers;
using GhJSON.Grasshopper.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.ScriptComponents;
using GhJSON.Grasshopper.Serialization.Shared;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Deserializes GhJSON documents to Grasshopper objects.
    /// </summary>
    public static partial class GhJsonDeserializer
    {
        /// <summary>
        /// Deserializes a GhJsonDocument to Grasshopper objects.
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Deserialization options.</param>
        /// <returns>The deserialization result containing the created components.</returns>
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options = null)
        {
            return Deserialize(document, options, ComponentHandlerRegistry.Default);
        }

        /// <summary>
        /// Deserializes a GhJsonDocument to Grasshopper objects using a custom handler registry.
        /// </summary>
        /// <param name="document">The document to deserialize.</param>
        /// <param name="options">Deserialization options.</param>
        /// <param name="handlerRegistry">Custom handler registry for component deserialization.</param>
        /// <returns>The deserialization result containing the created components.</returns>
        public static DeserializationResult Deserialize(
            GhJsonDocument document,
            DeserializationOptions? options,
            ComponentHandlerRegistry handlerRegistry)
        {
            options ??= DeserializationOptions.Standard;
            handlerRegistry ??= ComponentHandlerRegistry.Default;

            GeometricSerializerRegistry.Initialize();
            var result = new DeserializationResult
            {
                Options = options,
                Document = document
            };
            var idToObject = new Dictionary<int, IGH_DocumentObject>();
            var guidMapping = new Dictionary<Guid, IGH_DocumentObject>();

            foreach (var componentProps in document.Components)
            {
                try
                {
                    var obj = CreateComponent(componentProps, options, handlerRegistry);
                    if (obj != null)
                    {
                        result.Components.Add(obj);
                        if (componentProps.Id > 0)
                        {
                            idToObject[componentProps.Id] = obj;
                        }

                        // Build GUID mapping for canvas operations
                        if (componentProps.InstanceGuid.HasValue)
                        {
                            guidMapping[componentProps.InstanceGuid.Value] = obj;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create component '{componentProps.Name}': {ex.Message}");
                }
            }

            // Store GUID mapping for connection/group creation
            result.GuidMapping = guidMapping;
            result.IdMapping = idToObject;

            return result;
        }

        private static IGH_DocumentObject? CreateComponent(
            ComponentProperties props,
            DeserializationOptions options,
            ComponentHandlerRegistry handlerRegistry)
        {
            IGH_ObjectProxy? proxy = null;

            // Prefer GUID-first lookup to match SmartHopper behavior and avoid ambiguous name matches.
            if (props.ComponentGuid != Guid.Empty)
            {
                proxy = Instances.ComponentServer.EmitObjectProxy(props.ComponentGuid);
            }

            if (proxy == null)
            {
                proxy = Instances.ComponentServer.FindObjectByName(props.Name, true, true);
            }

            if (proxy == null)
            {
                throw new InvalidOperationException($"Component '{props.Name}' not found");
            }

            var obj = proxy.CreateInstance();
            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to instantiate component '{props.Name}'");
            }

            // Initialize attributes but do not set pivot here.
            // Placement (positioning + adding to canvas) must be handled by the caller (e.g., SmartHopper).
            obj.CreateAttributes();

            // Set instance GUID if specified
            if (props.InstanceGuid.HasValue && props.InstanceGuid.Value != Guid.Empty && options.PreserveInstanceGuids)
            {
                obj.NewInstanceGuid(props.InstanceGuid.Value);
            }

            if (obj is IGH_Component component)
            {
                // Apply nickname
                if (!string.IsNullOrEmpty(props.NickName))
                {
                    component.NickName = props.NickName;
                }

                if (IsScriptComponent(component))
                {
                    ApplyScriptComponentProperties(component, props, options);
                }
                else if (options.ApplyParameterSettings)
                {
                    // Apply parameter settings for standard (non-script) components.
                    // This is required so modifiers such as Reverse/Simplify, Expressions, and principal input
                    // round-trip when params belong to a component.
                    try
                    {
                        if (props.InputSettings != null && props.InputSettings.Any())
                        {
                            for (int i = 0; i < Math.Min(props.InputSettings.Count, component.Params.Input.Count); i++)
                            {
                                var s = props.InputSettings[i];
                                if (s != null)
                                {
                                    ParameterMapper.ApplySettings(component.Params.Input[i], s);
                                }

                                if (s?.IsPrincipal == true)
                                {
                                    component.MasterParameterIndex = i;
                                }
                            }
                        }

                        if (props.OutputSettings != null && props.OutputSettings.Any())
                        {
                            for (int i = 0; i < Math.Min(props.OutputSettings.Count, component.Params.Output.Count); i++)
                            {
                                var s = props.OutputSettings[i];
                                if (s != null)
                                {
                                    ParameterMapper.ApplySettings(component.Params.Output[i], s);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[GhJsonDeserializer] Error applying parameter settings for '{props.Name}': {ex.Message}");
                    }
                }
            }

            // Apply component state after script code/parameter rebuild to avoid re-generation.
            if (props.ComponentState != null && options.ApplyComponentState)
            {
                // Schema properties are now stored in componentState.additionalProperties.
                // Gate applying additionalProperties behind ApplyAdditionalProperties.
                var stateToApply = props.ComponentState;
                if (!options.ApplyAdditionalProperties && props.ComponentState.AdditionalProperties != null)
                {
                    stateToApply = new ComponentState
                    {
                        Value = props.ComponentState.Value,
                        VBCode = props.ComponentState.VBCode,
                        Selected = props.ComponentState.Selected,
                        Locked = props.ComponentState.Locked,
                        Hidden = props.ComponentState.Hidden,
                        Multiline = props.ComponentState.Multiline,
                        Wrap = props.ComponentState.Wrap,
                        Color = props.ComponentState.Color,
                        MarshInputs = props.ComponentState.MarshInputs,
                        MarshOutputs = props.ComponentState.MarshOutputs,
                        ShowStandardOutput = props.ComponentState.ShowStandardOutput,
                        ListMode = props.ComponentState.ListMode,
                        ListItems = props.ComponentState.ListItems,
                        Font = props.ComponentState.Font,
                        Corners = props.ComponentState.Corners,
                        DrawIndices = props.ComponentState.DrawIndices,
                        DrawPaths = props.ComponentState.DrawPaths,
                        Alignment = props.ComponentState.Alignment,
                        SpecialCodes = props.ComponentState.SpecialCodes,
                        Bounds = props.ComponentState.Bounds,
                        Rounding = props.ComponentState.Rounding,
                        AdditionalProperties = null,
                    };
                }

                handlerRegistry.ApplyState(obj, stateToApply);
            }

            return obj;
        }

        private static bool IsScriptComponent(IGH_Component component)
        {
            return ScriptComponentFactory.IsScriptComponent(component) ||
                   ScriptComponentHelper.IsScriptComponentInstance(component);
        }

        private static void ApplyScriptComponentProperties(
            IGH_Component component,
            ComponentProperties props,
            DeserializationOptions options)
        {
            var lang = ScriptComponentHelper.GetScriptLanguageTypeFromComponent(component);

            // STEP 1: Apply script code FIRST.
            if (lang == ScriptLanguage.VB && props.ComponentState?.VBCode != null)
            {
                ApplyVBScriptCode(component, props.ComponentState.VBCode);
            }
            else
            {
                var scriptCode = props.ComponentState?.Value?.ToString();
                if (!string.IsNullOrEmpty(scriptCode))
                {
                    if (options.InjectScriptTypeHints && lang == ScriptLanguage.CSharp)
                    {
                        scriptCode = InjectTypeHintsIntoCSharpRunScript(scriptCode, props);
                    }

                    ApplyScriptCode(component, scriptCode);

                    if (component is IGH_VariableParameterComponent varParamComp1)
                    {
                        varParamComp1.VariableParameterMaintenance();
                    }
                }
            }

            // STEP 2: Rebuild parameters from settings (if present).
            if (options.ApplyParameterSettings)
            {
                if (props.InputSettings != null && props.InputSettings.Any())
                {
                    component.Params.Input.Clear();
                    for (int i = 0; i < props.InputSettings.Count; i++)
                    {
                        var settings = props.InputSettings[i];
                        var param = ScriptParameterMapper.CreateParameter(settings, "input", lang, isOutput: false);
                        if (param != null)
                        {
                            component.Params.RegisterInputParam(param);

                            // Re-apply settings after registration (some script params recreate internal state)
                            var registered = component.Params.Input[i];
                            ScriptParameterMapper.ApplySettings(registered, settings);
                            if (!string.IsNullOrEmpty(settings.TypeHint))
                            {
                                ScriptParameterMapper.ApplyTypeHintToParameter(registered, settings.TypeHint);
                            }
                        }
                    }

                    for (int i = 0; i < Math.Min(props.InputSettings.Count, component.Params.Input.Count); i++)
                    {
                        if (props.InputSettings[i]?.IsPrincipal == true)
                        {
                            component.MasterParameterIndex = i;
                            break;
                        }
                    }
                }

                if (props.OutputSettings != null && props.OutputSettings.Any())
                {
                    component.Params.Output.Clear();
                    for (int i = 0; i < props.OutputSettings.Count; i++)
                    {
                        var settings = props.OutputSettings[i];
                        var param = ScriptParameterMapper.CreateParameter(settings, "output", lang, isOutput: true);
                        if (param != null)
                        {
                            component.Params.RegisterOutputParam(param);

                            var registered = component.Params.Output[i];
                            ScriptParameterMapper.ApplySettings(registered, settings);
                            if (!string.IsNullOrEmpty(settings.TypeHint))
                            {
                                ScriptParameterMapper.ApplyTypeHintToParameter(registered, settings.TypeHint);
                            }
                        }
                    }
                }
            }

            // STEP 3: Apply ShowStandardOutput AFTER parameters are configured.
            if (options.ApplyComponentState)
            {
                TryApplyShowStandardOutput(component, props.ComponentState);
            }

            component.CreateAttributes();
        }

        private static void TryApplyShowStandardOutput(IGH_Component component, ComponentState? state)
        {
            try
            {
                var compType = component.GetType();
                var usingStdOutputProp = compType.GetProperty("UsingStandardOutputParam");
                if (usingStdOutputProp == null || !usingStdOutputProp.CanWrite)
                    return;

                bool desired = state?.ShowStandardOutput ?? true;
                bool current = (bool)usingStdOutputProp.GetValue(component);

                if (current == desired)
                {
                    usingStdOutputProp.SetValue(component, !desired);
                    if (component is IGH_VariableParameterComponent varParamComp2)
                    {
                        varParamComp2.VariableParameterMaintenance();
                    }
                }

                usingStdOutputProp.SetValue(component, desired);
                if (component is IGH_VariableParameterComponent varParamComp3)
                {
                    varParamComp3.VariableParameterMaintenance();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonDeserializer] Error applying UsingStandardOutputParam: {ex.Message}");
            }
        }

        private static void ApplyVBScriptCode(IGH_Component component, VBScriptCode vbCode)
        {
            try
            {
                var compType = component.GetType();
                var scriptSourceProp = compType.GetProperty("ScriptSource");
                if (scriptSourceProp == null || !scriptSourceProp.CanRead)
                    return;

                var scriptSourceObj = scriptSourceProp.GetValue(component);
                if (scriptSourceObj == null)
                    return;

                var scriptSourceType = scriptSourceObj.GetType();
                var usingCodeProp = scriptSourceType.GetProperty("UsingCode");
                var scriptCodeProp = scriptSourceType.GetProperty("ScriptCode");
                var additionalCodeProp = scriptSourceType.GetProperty("AdditionalCode");

                if (usingCodeProp != null && usingCodeProp.CanWrite && vbCode.Imports != null)
                {
                    usingCodeProp.SetValue(scriptSourceObj, vbCode.Imports);
                }

                if (scriptCodeProp != null && scriptCodeProp.CanWrite && vbCode.Script != null)
                {
                    scriptCodeProp.SetValue(scriptSourceObj, vbCode.Script);
                }

                if (additionalCodeProp != null && additionalCodeProp.CanWrite && vbCode.Additional != null)
                {
                    additionalCodeProp.SetValue(scriptSourceObj, vbCode.Additional);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonDeserializer] Error applying VB script code: {ex.Message}");
            }
        }

        private static string InjectTypeHintsIntoCSharpRunScript(string scriptCode, ComponentProperties props)
        {
            try
            {
                if (props.InputSettings != null)
                {
                    foreach (var s in props.InputSettings)
                    {
                        scriptCode = ReplaceCSharpObjectParamType(scriptCode, s);
                    }
                }

                if (props.OutputSettings != null)
                {
                    foreach (var s in props.OutputSettings)
                    {
                        scriptCode = ReplaceCSharpObjectParamType(scriptCode, s);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonDeserializer] Error injecting C# type hints: {ex.Message}");
            }

            return scriptCode;
        }

        private static string ReplaceCSharpObjectParamType(string scriptCode, ParameterSettings settings)
        {
            if (string.IsNullOrEmpty(settings?.TypeHint))
                return scriptCode;

            var baseType = TypeHintMapper.ExtractBaseType(settings.TypeHint) ?? settings.TypeHint;
            if (string.IsNullOrEmpty(baseType) || string.Equals(baseType, "object", StringComparison.OrdinalIgnoreCase))
                return scriptCode;

            var rawName = settings.VariableName ?? settings.ParameterName;
            if (string.IsNullOrEmpty(rawName))
                return scriptCode;

            var name = SanitizeCSharpIdentifier(rawName);

            scriptCode = Regex.Replace(
                scriptCode,
                $@"\bref\s+object\s+{Regex.Escape(name)}\b",
                $"ref {baseType} {name}");

            scriptCode = Regex.Replace(
                scriptCode,
                $@"\bout\s+object\s+{Regex.Escape(name)}\b",
                $"out {baseType} {name}");

            scriptCode = Regex.Replace(
                scriptCode,
                $@"\bobject\s+{Regex.Escape(name)}\b",
                $"{baseType} {name}");

            return scriptCode;
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

        private static void ApplyScriptCode(IGH_Component component, string? scriptCode)
        {
            if (string.IsNullOrEmpty(scriptCode))
                return;

            try
            {
                var type = component.GetType();

                // Legacy (integrated) implementation used RhinoCodePlatform.GH.IScriptComponent.Text.
                // That property is often an explicit interface implementation, so it won't appear as
                // a public property on the concrete component type.
                try
                {
                    var scriptInterface = type.GetInterfaces().FirstOrDefault(i => i.Name == "IScriptComponent");
                    if (scriptInterface != null)
                    {
                        var textProp = scriptInterface.GetProperty("Text");
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

                // Try common direct properties first
                string[] candidates = { "Text", "Script", "Code", "ScriptCode", "Source", "SourceCode" };
                foreach (var name in candidates)
                {
                    var prop = type.GetProperty(name);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(component, scriptCode);
                        return;
                    }
                }

                // Rhino 8 script components often use a ScriptSource object
                var scriptSourceProp = type.GetProperty("ScriptSource");
                if (scriptSourceProp != null && scriptSourceProp.CanRead)
                {
                    var scriptSourceObj = scriptSourceProp.GetValue(component);
                    if (scriptSourceObj != null)
                    {
                        var scriptSourceType = scriptSourceObj.GetType();
                        string[] sourceCandidates = { "ScriptCode", "Code", "Text", "Source", "SourceCode" };
                        foreach (var name in sourceCandidates)
                        {
                            var prop = scriptSourceType.GetProperty(name);
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
                Debug.WriteLine($"[GhJsonDeserializer] Error applying script code: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Result of a deserialization operation.
    /// </summary>
    public class DeserializationResult
    {
        /// <summary>
        /// Gets the list of created components.
        /// </summary>
        public List<IGH_DocumentObject> Components { get; } = new List<IGH_DocumentObject>();

        /// <summary>
        /// Gets the list of errors that occurred during deserialization.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of warnings that occurred during deserialization.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether deserialization was successful (no errors).
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;

        /// <summary>
        /// Gets or sets the deserialization options used.
        /// </summary>
        public DeserializationOptions? Options { get; set; }

        /// <summary>
        /// Gets or sets the mapping from instance GUIDs to created component instances.
        /// Used for connection and group creation after component instantiation.
        /// </summary>
        public Dictionary<Guid, IGH_DocumentObject> GuidMapping { get; set; } = new Dictionary<Guid, IGH_DocumentObject>();

        /// <summary>
        /// Gets or sets the mapping from compact integer IDs to created component instances.
        /// This is required to support documents that omit <c>instanceGuid</c> but include <c>id</c>.
        /// </summary>
        public Dictionary<int, IGH_DocumentObject> IdMapping { get; set; } = new Dictionary<int, IGH_DocumentObject>();

        /// <summary>
        /// Gets or sets the original GhJSON document that was deserialized.
        /// Useful for accessing component properties during placement.
        /// </summary>
        public GhJsonDocument? Document { get; set; }
    }

}
