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

using GhJSON.Core.SchemaModels;

using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    internal sealed class VBScriptHandler : BaseScriptHandler
    {
        private static readonly Guid VBNetGuid = new Guid("079bd9bd-54a0-41d4-98af-db999015f63d");

        public override string ExtensionKey => "gh.vbscript";

        protected override Guid ComponentGuid => VBNetGuid;

        protected override string ComponentName => "VB Script";

        public override void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
            if (obj is not IGH_Component scriptComp)
            {
                return;
            }

            component.ComponentState ??= new GhJsonComponentState();
            component.ComponentState.Extensions ??= new Dictionary<string, object>();

            var vbCode = ExtractVBScriptCode(scriptComp);
            if (vbCode == null)
            {
                return;
            }

            var data = new Dictionary<string, object>
            {
                ["vbCode"] = vbCode,
            };

            if (TryReadUsingStandardOutputParam(scriptComp, out var showStdOutput))
            {
                data["showStandardOutput"] = showStdOutput;
            }

            component.ComponentState.Extensions[ExtensionKey] = data;
        }

        public override void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is not IGH_Component scriptComp ||
                component.ComponentState?.Extensions == null ||
                !component.ComponentState.Extensions.TryGetValue(ExtensionKey, out var extData) ||
                extData is not Dictionary<string, object> data)
            {
                return;
            }

            if (data.TryGetValue("vbCode", out var vbCodeObj) && vbCodeObj is Dictionary<string, object> vbCode)
            {
                ApplyVBScriptCode(scriptComp, vbCode);
            }

            if (data.TryGetValue("showStandardOutput", out var showObj) && bool.TryParse(showObj?.ToString(), out var desired))
            {
                TryApplyUsingStandardOutputParam(scriptComp, desired);
            }
        }

        private static Dictionary<string, object>? ExtractVBScriptCode(IGH_Component component)
        {
            try
            {
                var componentType = component.GetType();
                var scriptSourceProp = componentType.GetProperty("ScriptSource");
                if (scriptSourceProp == null || !scriptSourceProp.CanRead)
                {
                    return null;
                }

                var scriptSourceObj = scriptSourceProp.GetValue(component);
                if (scriptSourceObj == null)
                {
                    return null;
                }

                var scriptSourceType = scriptSourceObj.GetType();
                var usingCodeProp = scriptSourceType.GetProperty("UsingCode");
                var scriptCodeProp = scriptSourceType.GetProperty("ScriptCode");
                var additionalCodeProp = scriptSourceType.GetProperty("AdditionalCode");

                var vbCode = new Dictionary<string, object>();

                var imports = usingCodeProp?.GetValue(scriptSourceObj) as string;
                if (!string.IsNullOrEmpty(imports))
                {
                    vbCode["imports"] = imports;
                }

                var script = scriptCodeProp?.GetValue(scriptSourceObj) as string;
                if (!string.IsNullOrEmpty(script))
                {
                    vbCode["script"] = script;
                }

                var additional = additionalCodeProp?.GetValue(scriptSourceObj) as string;
                if (!string.IsNullOrEmpty(additional))
                {
                    vbCode["additional"] = additional;
                }

                return vbCode.Count > 0 ? vbCode : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VBScriptHandler] Error extracting VB script code: {ex.Message}");
                return null;
            }
        }

        private static void ApplyVBScriptCode(IGH_Component component, Dictionary<string, object> vbCode)
        {
            try
            {
                var compType = component.GetType();
                var scriptSourceProp = compType.GetProperty("ScriptSource");
                if (scriptSourceProp == null || !scriptSourceProp.CanRead)
                {
                    return;
                }

                var scriptSourceObj = scriptSourceProp.GetValue(component);
                if (scriptSourceObj == null)
                {
                    return;
                }

                var scriptSourceType = scriptSourceObj.GetType();
                var usingCodeProp = scriptSourceType.GetProperty("UsingCode");
                var scriptCodeProp = scriptSourceType.GetProperty("ScriptCode");
                var additionalCodeProp = scriptSourceType.GetProperty("AdditionalCode");

                if (usingCodeProp != null && usingCodeProp.CanWrite && vbCode.TryGetValue("imports", out var importsObj))
                {
                    usingCodeProp.SetValue(scriptSourceObj, importsObj?.ToString());
                }

                if (scriptCodeProp != null && scriptCodeProp.CanWrite && vbCode.TryGetValue("script", out var scriptObj))
                {
                    scriptCodeProp.SetValue(scriptSourceObj, scriptObj?.ToString());
                }

                if (additionalCodeProp != null && additionalCodeProp.CanWrite && vbCode.TryGetValue("additional", out var additionalObj))
                {
                    additionalCodeProp.SetValue(scriptSourceObj, additionalObj?.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VBScriptHandler] Error applying VB script code: {ex.Message}");
            }
        }
    }
}
