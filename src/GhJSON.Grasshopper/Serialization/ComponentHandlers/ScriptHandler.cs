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
using GhJSON.Core.Models.Components;
using GhJSON.Grasshopper.Serialization.ScriptComponents;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    /// <summary>
    /// Handler for script components (C#, Python, IronPython, VB.NET).
    /// Serializes script code, VB sections, and standard output visibility.
    /// </summary>
    public class ScriptHandler : IComponentHandler
    {
        /// <inheritdoc/>
        public IEnumerable<Guid> SupportedComponentGuids => new[]
        {
            ScriptComponentFactory.Python3Guid,
            ScriptComponentFactory.IronPython2Guid,
            ScriptComponentFactory.CSharpGuid,
            ScriptComponentFactory.VBNetGuid
        };

        /// <inheritdoc/>
        public IEnumerable<Type> SupportedTypes => Array.Empty<Type>();

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component component)
                return false;

            return ScriptComponentFactory.IsScriptComponent(component) ||
                   ScriptComponentHelper.IsScriptComponentInstance(component);
        }

        /// <inheritdoc/>
        public ComponentState? ExtractState(IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component component || !CanHandle(obj))
                return null;

            var state = new ComponentState();
            bool hasState = false;

            var lang = ScriptComponentHelper.GetScriptLanguageTypeFromComponent(component);

            // VB Script uses 3 separate code sections
            if (lang == ScriptLanguage.VB)
            {
                var vbCode = ExtractVBScriptCode(component);
                if (vbCode != null)
                {
                    state.VBCode = vbCode;
                    hasState = true;
                }
            }
            else
            {
                // Extract script code for non-VB scripts
                var scriptCode = ExtractScriptCode(component);
                if (!string.IsNullOrEmpty(scriptCode))
                {
                    state.Value = scriptCode;
                    hasState = true;
                }
            }

            // Extract standard output visibility ("out" param)
            try
            {
                var usingStdOutputProp = component.GetType().GetProperty("UsingStandardOutputParam");
                if (usingStdOutputProp != null && usingStdOutputProp.CanRead)
                {
                    var value = usingStdOutputProp.GetValue(component) as bool?;
                    if (value.HasValue)
                    {
                        state.ShowStandardOutput = value.Value;
                        hasState = true;
                    }
                }
            }
            catch
            {
            }

            // Extract locked state
            if (component.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract hidden state
            if (component.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            return hasState ? state : null;
        }

        /// <inheritdoc/>
        public object? ExtractValue(IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component component || !CanHandle(obj))
                return null;

            var lang = ScriptComponentHelper.GetScriptLanguageTypeFromComponent(component);

            // VB Script returns VBScriptCode, not a simple string value
            if (lang == ScriptLanguage.VB)
            {
                return ExtractVBScriptCode(component);
            }

            return ExtractScriptCode(component);
        }

        /// <inheritdoc/>
        public void ApplyState(IGH_DocumentObject obj, ComponentState state)
        {
            if (obj is not IGH_Component component || state == null || !CanHandle(obj))
                return;

            var lang = ScriptComponentHelper.GetScriptLanguageTypeFromComponent(component);

            // Apply VB script code sections
            if (lang == ScriptLanguage.VB && state.VBCode != null)
            {
                ApplyVBScriptCode(component, state.VBCode);
            }
            // Apply standard script code
            else if (state.Value != null)
            {
                ApplyValue(obj, state.Value);
            }

            // Apply standard output visibility
            if (state.ShowStandardOutput.HasValue)
            {
                TryApplyShowStandardOutput(component, state.ShowStandardOutput.Value);
            }

            // Apply locked state
            if (state.Locked.HasValue)
            {
                component.Locked = state.Locked.Value;
            }

            // Apply hidden state
            if (state.Hidden.HasValue)
            {
                component.Hidden = state.Hidden.Value;
            }
        }

        /// <inheritdoc/>
        public void ApplyValue(IGH_DocumentObject obj, object value)
        {
            if (obj is not IGH_Component component || value == null || !CanHandle(obj))
                return;

            // Handle VBScriptCode object
            if (value is VBScriptCode vbCode)
            {
                ApplyVBScriptCode(component, vbCode);
                return;
            }

            // Apply script code string
            var scriptCode = value.ToString();
            if (string.IsNullOrEmpty(scriptCode))
                return;

            ApplyScriptCode(component, scriptCode);
        }

        #region Script Code Extraction

        private static string? ExtractScriptCode(IGH_Component component)
        {
            try
            {
                // Try IScriptComponent.Text interface first (explicit interface implementation)
                try
                {
                    var typeOfScript = component.GetType();
                    var scriptInterface = typeOfScript.GetInterfaces().FirstOrDefault(i => i.Name == "IScriptComponent");
                    if (scriptInterface != null)
                    {
                        var textProp = scriptInterface.GetProperty("Text");
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

                // Try common script code property names
                var type = component.GetType();
                string[] candidates = { "Text", "Script", "Code", "ScriptCode", "Source", "SourceCode" };

                foreach (var name in candidates)
                {
                    var prop = type.GetProperty(name);
                    if (prop != null && prop.CanRead)
                    {
                        var value = prop.GetValue(component)?.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }

                // Rhino 8 script components often expose code through a ScriptSource object
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
                Debug.WriteLine($"[ScriptHandler] Error extracting script code: {ex.Message}");
            }

            return null;
        }

        private static VBScriptCode? ExtractVBScriptCode(IGH_Component component)
        {
            try
            {
                var componentType = component.GetType();
                var scriptSourceProp = componentType.GetProperty("ScriptSource");
                if (scriptSourceProp == null || !scriptSourceProp.CanRead)
                    return null;

                var scriptSourceObj = scriptSourceProp.GetValue(component);
                if (scriptSourceObj == null)
                    return null;

                var scriptSourceType = scriptSourceObj.GetType();
                var usingCodeProp = scriptSourceType.GetProperty("UsingCode");
                var scriptCodeProp = scriptSourceType.GetProperty("ScriptCode");
                var additionalCodeProp = scriptSourceType.GetProperty("AdditionalCode");

                return new VBScriptCode
                {
                    Imports = usingCodeProp?.GetValue(scriptSourceObj) as string,
                    Script = scriptCodeProp?.GetValue(scriptSourceObj) as string,
                    Additional = additionalCodeProp?.GetValue(scriptSourceObj) as string,
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScriptHandler] Error extracting VB script code: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Script Code Application

        private static void ApplyScriptCode(IGH_Component component, string scriptCode)
        {
            try
            {
                var type = component.GetType();

                // Try IScriptComponent.Text interface first
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

                // Try common direct properties
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
                Debug.WriteLine($"[ScriptHandler] Error applying script code: {ex.Message}");
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
                Debug.WriteLine($"[ScriptHandler] Error applying VB script code: {ex.Message}");
            }
        }

        private static void TryApplyShowStandardOutput(IGH_Component component, bool desired)
        {
            try
            {
                var compType = component.GetType();
                var usingStdOutputProp = compType.GetProperty("UsingStandardOutputParam");
                if (usingStdOutputProp == null || !usingStdOutputProp.CanWrite)
                    return;

                bool current = (bool)usingStdOutputProp.GetValue(component);

                // Toggle to trigger parameter update if needed
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
                Debug.WriteLine($"[ScriptHandler] Error applying UsingStandardOutputParam: {ex.Message}");
            }
        }

        #endregion
    }
}
