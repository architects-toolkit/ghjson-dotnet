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
    internal abstract class BaseScriptHandler : IObjectHandler
    {
        public int Priority => 100;

        public string? SchemaExtensionUrl => null;

        public abstract string ExtensionKey { get; }

        protected abstract Guid ComponentGuid { get; }

        protected abstract string ComponentName { get; }

        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component comp && comp.ComponentGuid == this.ComponentGuid;
        }

        public bool CanHandle(GhJsonComponent component)
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
    }
}
