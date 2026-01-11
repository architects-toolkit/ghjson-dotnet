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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;
using GhJSON.Core.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.DataTypes;
using GhJSON.Grasshopper.Serialization.ScriptComponents;
using GhJSON.Grasshopper.Serialization.SchemaProperties;
using GhJSON.Grasshopper.Serialization.SchemaProperties.PropertyFilters;
using GhJSON.Grasshopper.Serialization.Shared;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Serializes Grasshopper canvas objects to GhJSON format.
    /// </summary>
    public static class GhJsonSerializer
    {
        /// <summary>
        /// Serializes a collection of Grasshopper objects to a GrasshopperDocument.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>A GrasshopperDocument representing the serialized objects.</returns>
        public static GrasshopperDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options = null)
        {
            options ??= SerializationOptions.Standard;
            GeometricSerializerRegistry.Initialize();
            var objectList = objects.ToList();
            var document = new GrasshopperDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>(),
                Connections = new List<ConnectionPairing>()
            };

            // Build GUID to ID mapping
            var guidToId = new Dictionary<Guid, int>();
            int nextId = 1;

            foreach (var obj in objectList)
            {
                guidToId[obj.InstanceGuid] = nextId++;
            }

            // Serialize components
            foreach (var obj in objectList)
            {
                var props = SerializeObject(obj, guidToId, options);
                if (props != null)
                {
                    document.Components.Add(props);
                }
            }

            // Extract connections
            if (options.IncludeConnections)
            {
                document.Connections = ExtractConnections(objectList, guidToId);
            }

            // Add metadata if requested
            if (options.IncludeMetadata)
            {
                document.Metadata = CreateMetadata(objectList);
            }

            // Extract groups
            if (options.IncludeGroups)
            {
                ExtractGroupInformation(document, guidToId);
            }

            return document;
        }

        private static void ExtractGroupInformation(
            GrasshopperDocument document,
            Dictionary<Guid, int> guidToId)
        {
            try
            {
                var canvas = Instances.ActiveCanvas;
                if (canvas?.Document == null)
                    return;

                var groups = canvas.Document.Objects.OfType<GH_Group>().ToList();
                if (groups.Count == 0)
                    return;

                document.Groups = new List<GroupInfo>();

                foreach (var group in groups)
                {
                    var groupInfo = new GroupInfo
                    {
                        InstanceGuid = group.InstanceGuid,
                        Name = group.NickName,
                        Color = DataTypeSerializer.Serialize(group.Colour)
                    };

                    var memberIds = new List<int>();
                    foreach (var member in group.Objects())
                    {
                        if (member is IGH_DocumentObject objDo &&
                            guidToId.TryGetValue(objDo.InstanceGuid, out var id))
                        {
                            memberIds.Add(id);
                        }
                    }

                    groupInfo.Members = memberIds;

                    if (memberIds.Count > 0)
                    {
                        document.Groups.Add(groupInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonSerializer] Error extracting groups: {ex.Message}");
            }
        }

        private static ComponentProperties? SerializeObject(
            IGH_ActiveObject obj,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            if (obj is IGH_Component component)
            {
                return SerializeComponent(component, guidToId, options);
            }
            else if (obj is IGH_Param param)
            {
                return SerializeParameter(param, guidToId, options);
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
                Debug.WriteLine($"[GhJsonSerializer] Error extracting VB script code: {ex.Message}");
                return null;
            }
        }

        private static ComponentProperties SerializeComponent(
            IGH_Component component,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            var attrs = component.Attributes;
            var pivot = attrs?.Pivot ?? new System.Drawing.PointF(0, 0);

            var props = new ComponentProperties
            {
                Name = component.Name,
                NickName = component.NickName != component.Name ? component.NickName : null,
                ComponentGuid = component.ComponentGuid,
                InstanceGuid = component.InstanceGuid,
                Id = guidToId.TryGetValue(component.InstanceGuid, out var id) ? id : 0,
                Pivot = new CompactPosition(pivot.X, pivot.Y)
            };

            // Extract component state
            if (options.IncludeComponentState)
            {
                props.ComponentState = ExtractComponentState(component);
            }

            // Extract parameter settings
            if (options.IncludeParameterSettings)
            {
                // Use script-aware extraction for script components
                if (ScriptComponentFactory.IsScriptComponent(component) ||
                    ScriptComponentHelper.IsScriptComponentInstance(component))
                {
                    props.InputSettings = ExtractScriptParameterSettings(component.Params.Input, component);
                    props.OutputSettings = ExtractScriptParameterSettings(component.Params.Output, component);
                }
                else
                {
                    props.InputSettings = ExtractParameterSettings(component.Params.Input, component, isInput: true);
                    props.OutputSettings = ExtractParameterSettings(component.Params.Output, component, isInput: false);
                }
            }

            // Extract schema properties (legacy format)
            if (options.IncludeSchemaProperties)
            {
                var ctx = GetSchemaPropertyContext(options);
                var manager = new PropertyManagerV2(ctx);
                var schemaProps = manager.ExtractProperties(component);
                if (schemaProps.Count > 0)
                {
                    props.SchemaProperties = schemaProps;
                }
            }

            return props;
        }

        private static ComponentProperties SerializeParameter(
            IGH_Param param,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options)
        {
            var attrs = param.Attributes;
            var pivot = attrs?.Pivot ?? new System.Drawing.PointF(0, 0);

            var props = new ComponentProperties
            {
                Name = param.Name,
                NickName = param.NickName != param.Name ? param.NickName : null,
                ComponentGuid = param.ComponentGuid,
                InstanceGuid = param.InstanceGuid,
                Id = guidToId.TryGetValue(param.InstanceGuid, out var id) ? id : 0,
                Pivot = new CompactPosition(pivot.X, pivot.Y)
            };

            if (options.IncludeSchemaProperties)
            {
                var ctx = GetSchemaPropertyContext(options);
                var manager = new PropertyManagerV2(ctx);
                var schemaProps = manager.ExtractProperties(param);
                if (schemaProps.Count > 0)
                {
                    props.SchemaProperties = schemaProps;
                }
            }

            return props;
        }

        private static SerializationContext GetSchemaPropertyContext(SerializationOptions options)
        {
            if (!options.IncludeComponentState && !options.IncludeParameterSettings)
                return SerializationContext.Lite;

            if (!options.IncludePersistentData)
                return SerializationContext.Optimized;

            return SerializationContext.Standard;
        }

        private static List<ConnectionPairing> ExtractConnections(
            List<IGH_ActiveObject> objects,
            Dictionary<Guid, int> guidToId)
        {
            var connections = new List<ConnectionPairing>();

            // Internal IGH_Param instances (component inputs/outputs) have their own InstanceGuid,
            // which is NOT the same as the owning component's InstanceGuid and is typically not
            // present in the top-level object list. To serialize connections reliably we must
            // resolve each param to its owning document object (component or stand-alone param).
            var paramOwner = new Dictionary<IGH_Param, IGH_ActiveObject>();

            foreach (var obj in objects)
            {
                if (obj is IGH_Param p)
                {
                    paramOwner[p] = obj;
                }
                else if (obj is IGH_Component c)
                {
                    foreach (var pIn in c.Params.Input)
                    {
                        paramOwner[pIn] = obj;
                    }

                    foreach (var pOut in c.Params.Output)
                    {
                        paramOwner[pOut] = obj;
                    }
                }
            }

            IGH_ActiveObject? ResolveOwner(IGH_Param p)
            {
                if (p == null)
                {
                    return null;
                }

                if (paramOwner.TryGetValue(p, out var owner))
                {
                    return owner;
                }

                return null;
            }

            foreach (var obj in objects)
            {
                if (obj is IGH_Component component)
                {
                    foreach (var input in component.Params.Input)
                    {
                        foreach (var source in input.Sources)
                        {
                            var fromOwner = ResolveOwner(source);

                            if (fromOwner == null)
                            {
                                continue;
                            }

                            if (guidToId.TryGetValue(fromOwner.InstanceGuid, out var fromId) &&
                                guidToId.TryGetValue(component.InstanceGuid, out var toId))
                            {
                                var fromParamName = source.NickName;
                                var toParamName = input.NickName;

                                int? fromParamIndex = null;
                                if (fromOwner is IGH_Component fromComp)
                                {
                                    var idx = fromComp.Params.Output.IndexOf(source);
                                    if (idx >= 0)
                                    {
                                        fromParamIndex = idx;
                                    }
                                }

                                var toParamIndex = component.Params.Input.IndexOf(input);

                                connections.Add(new ConnectionPairing
                                {
                                    From = new Connection { Id = fromId, ParamName = fromParamName, ParamIndex = fromParamIndex },
                                    To = new Connection { Id = toId, ParamName = toParamName, ParamIndex = toParamIndex >= 0 ? toParamIndex : null }
                                });
                            }
                        }
                    }
                }
                else if (obj is IGH_Param param)
                {
                    foreach (var source in param.Sources)
                    {
                        var fromOwner = ResolveOwner(source);

                        if (fromOwner == null)
                        {
                            continue;
                        }

                        if (guidToId.TryGetValue(fromOwner.InstanceGuid, out var fromId) &&
                            guidToId.TryGetValue(param.InstanceGuid, out var toId))
                        {
                            var fromParamName = source.NickName;
                            var toParamName = param.NickName;

                            int? fromParamIndex = null;
                            if (fromOwner is IGH_Component fromComp)
                            {
                                var idx = fromComp.Params.Output.IndexOf(source);
                                if (idx >= 0)
                                {
                                    fromParamIndex = idx;
                                }
                            }

                            connections.Add(new ConnectionPairing
                            {
                                From = new Connection { Id = fromId, ParamName = fromParamName, ParamIndex = fromParamIndex },
                                To = new Connection { Id = toId, ParamName = toParamName }
                            });
                        }
                    }
                }
            }

            return connections;
        }

        public static JObject ExtractRuntimeData(IEnumerable<IGH_ActiveObject> objects)
        {
            var result = new JObject();

            foreach (var obj in objects)
            {
                var componentData = new JObject();

                if (obj is IGH_Component comp)
                {
                    var outputsData = new JObject();
                    foreach (var output in comp.Params.Output)
                    {
                        var paramData = ExtractParameterVolatileData(output);
                        if (paramData != null)
                        {
                            outputsData[output.NickName] = paramData;
                        }
                    }

                    if (outputsData.Count > 0)
                    {
                        componentData["outputs"] = outputsData;
                    }

                    var inputsData = new JObject();
                    foreach (var input in comp.Params.Input)
                    {
                        var paramData = ExtractParameterVolatileData(input);
                        if (paramData != null)
                        {
                            inputsData[input.NickName] = paramData;
                        }
                    }

                    if (inputsData.Count > 0)
                    {
                        componentData["inputs"] = inputsData;
                    }
                }
                else if (obj is IGH_Param param)
                {
                    var paramData = ExtractParameterVolatileData(param);
                    if (paramData != null)
                    {
                        componentData["data"] = paramData;
                    }
                }

                if (componentData.Count > 0)
                {
                    componentData["name"] = obj.Name;
                    result[obj.InstanceGuid.ToString()] = componentData;
                }
            }

            return result;
        }

        public static JObject? ExtractParameterVolatileData(IGH_Param param)
        {
            var volatileData = param.VolatileData;
            if (volatileData == null || volatileData.IsEmpty)
            {
                return null;
            }

            var paramData = new JObject
            {
                ["totalCount"] = volatileData.DataCount,
                ["branchCount"] = volatileData.PathCount,
            };

            var branches = new JArray();
            foreach (var path in volatileData.Paths)
            {
                var branch = volatileData.get_Branch(path);
                var branchInfo = new JObject
                {
                    ["path"] = path.ToString(),
                    ["count"] = branch?.Count ?? 0,
                };

                if (branch != null && branch.Count > 0)
                {
                    var samples = new JArray();
                    int sampleCount = Math.Min(branch.Count, 3);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        var item = branch[i];
                        if (item is IGH_Goo goo)
                        {
                            samples.Add(goo.ToString());
                        }
                        else if (item != null)
                        {
                            samples.Add(item.ToString());
                        }
                    }

                    if (samples.Count > 0)
                    {
                        branchInfo["samples"] = samples;
                    }

                    if (branch.Count > sampleCount)
                    {
                        branchInfo["hasMore"] = true;
                    }
                }

                branches.Add(branchInfo);
            }

            paramData["branches"] = branches;
            return paramData;
        }

        private static ComponentState? ExtractComponentState(IGH_Component component)
        {
            var state = new ComponentState();
            bool hasState = false;

            // Extract Locked state
            if (component.Locked)
            {
                state.Locked = true;
                hasState = true;
            }

            // Extract Hidden state
            if (component.Hidden)
            {
                state.Hidden = true;
                hasState = true;
            }

            // Extract universal value for special components
            var universalValue = ExtractUniversalValue(component);
            if (universalValue != null)
            {
                state.Value = universalValue;
                hasState = true;
            }

            // Extract bounds for UI components (panels, etc.)
            try
            {
                if (component is GH_Panel && component.Attributes != null)
                {
                    var b = component.Attributes.Bounds;
                    state.Bounds = new Dictionary<string, float>
                    {
                        ["width"] = b.Width,
                        ["height"] = b.Height,
                    };
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract color for UI components (panel, swatch)
            try
            {
                Color? c = null;

                if (component is GH_Panel panel)
                {
                    c = panel.Properties.Colour;
                }
                else if (component is GH_ColourSwatch swatch)
                {
                    c = swatch.SwatchColour;
                }

                if (c.HasValue)
                {
                    var col = c.Value;
                    state.Color = new Dictionary<string, int>
                    {
                        ["r"] = col.R,
                        ["g"] = col.G,
                        ["b"] = col.B,
                        ["a"] = col.A,
                    };
                    hasState = true;
                }
            }
            catch
            {
            }

            // Extract script-specific state
            if (ScriptComponentFactory.IsScriptComponent(component) ||
                ScriptComponentHelper.IsScriptComponentInstance(component))
            {
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

                // Standard output visibility ("out" param)
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
            }

            return hasState ? state : null;
        }

        private static object? ExtractUniversalValue(IGH_Component component)
        {
            try
            {
                // Number slider
                if (component is GH_NumberSlider slider)
                {
                    return FormatSliderValue(slider);
                }

                // Panel
                if (component is GH_Panel panel)
                {
                    return panel.UserText;
                }

                // Value list
                if (component is GH_ValueList valueList)
                {
                    return valueList.FirstSelectedItem?.Name;
                }

                // Boolean toggle
                if (component is GH_BooleanToggle toggle)
                {
                    return toggle.Value;
                }

                // Colour swatch
                if (component is GH_ColourSwatch swatch)
                {
                    var c = swatch.SwatchColour;
                    return $"rgba:{c.R},{c.G},{c.B},{c.A}";
                }

                // Button object
                if (component is GH_ButtonObject btn)
                {
                    var expNormal = btn.ExpressionNormal;
                    var expPressed = btn.ExpressionPressed;

                    // Only serialize if not default values
                    if (expNormal != "False" || expPressed != "True")
                    {
                        return new Dictionary<string, string>
                        {
                            { "normal", expNormal ?? "False" },
                            { "pressed", expPressed ?? "True" }
                        };
                    }
                }

                // Script components - check if it's a known script type
                if (ScriptComponentFactory.IsScriptComponent(component) ||
                    ScriptComponentHelper.IsScriptComponentInstance(component))
                {
                    var lang = ScriptComponentHelper.GetScriptLanguageTypeFromComponent(component);
                    if (lang != ScriptLanguage.VB)
                    {
                        return ExtractScriptCode(component);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonSerializer] Error extracting universal value: {ex.Message}");
            }

            return null;
        }

        private static string? ExtractScriptCode(IGH_Component component)
        {
            try
            {
                // Legacy (integrated) implementation relied on RhinoCodePlatform.GH.IScriptComponent.Text.
                // In ghjson-dotnet we avoid compile-time dependencies, so we use reflection and support
                // explicit interface implementations by reading the property from the interface type.
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

                // Try common script code property names across GH1 / Rhino 8 script components.
                // We keep this reflection-based so ghjson-dotnet doesn't need RhinoCodePlatform refs.
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

                // Rhino 8 script components often expose code through a ScriptSource object.
                // Mirror VB's approach but tolerate different property names.
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
                Debug.WriteLine($"[GhJsonSerializer] Error extracting script code: {ex.Message}");
            }

            return null;
        }

        private static string FormatSliderValue(GH_NumberSlider slider)
        {
            var current = slider.CurrentValue;
            var min = slider.Slider.Minimum;
            var max = slider.Slider.Maximum;

            // Format as "current<min~max>" for compact representation
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}<{1}~{2}>",
                current,
                min,
                max);
        }

        private static List<ParameterSettings>? ExtractScriptParameterSettings(
            List<IGH_Param> parameters,
            IGH_Component component)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            var result = new List<ParameterSettings>();
            var isPrincipalIndex = component is IGH_Component comp ? comp.Params.Input.IndexOf(comp.Params.Input.FirstOrDefault(p => comp.Params.Input.IndexOf(p) == comp.MasterParameterIndex)) : -1;

            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                var isPrincipal = (p.Kind == GH_ParamKind.input && i == component.MasterParameterIndex);
                var settings = ScriptParameterMapper.ExtractSettings(p, isPrincipal);
                if (settings != null)
                {
                    result.Add(settings);
                }
            }

            return result.Count > 0 ? result : null;
        }

        private static List<ParameterSettings>? ExtractParameterSettings(
            List<IGH_Param> parameters,
            IGH_Component owner,
            bool isInput)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            var result = new List<ParameterSettings>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                var isPrincipal = isInput && i == owner.MasterParameterIndex;
                var settings = ParameterMapper.ExtractSettings(p, isPrincipal);
                if (settings != null)
                {
                    result.Add(settings);
                }
            }

            return result.Count > 0 ? result : null;
        }

        private static object? ExtractPersistentData(IGH_Param param)
        {
            try
            {
                var data = param.VolatileData;
                if (data == null || data.IsEmpty)
                    return null;

                // For simple single-value parameters, return the value directly
                if (data.PathCount == 1 && data.DataCount == 1)
                {
                    var allData = data.AllData(true).FirstOrDefault();
                    if (allData != null)
                    {
                        // Try to get the underlying value
                        var valueProperty = allData.GetType().GetProperty("Value");
                        if (valueProperty != null)
                        {
                            return valueProperty.GetValue(allData);
                        }
                        return allData.ToString();
                    }
                }

                // For complex data trees, return a summary
                return new Dictionary<string, object>
                {
                    ["pathCount"] = data.PathCount,
                    ["dataCount"] = data.DataCount
                };
            }
            catch
            {
                return null;
            }
        }

        private static DocumentMetadata CreateMetadata(List<IGH_ActiveObject> objects)
        {
            return new DocumentMetadata
            {
                ComponentCount = objects.Count,
                PluginVersion = "1.0.0"
            };
        }
    }
}
