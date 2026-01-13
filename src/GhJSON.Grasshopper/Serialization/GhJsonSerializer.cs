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
using GhJSON.Grasshopper.Serialization.ComponentHandlers;
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
        /// Serializes a collection of Grasshopper objects to a GhJsonDocument.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>A GhJsonDocument representing the serialized objects.</returns>
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options = null)
        {
            return Serialize(objects, options, ComponentHandlerRegistry.Default);
        }

        /// <summary>
        /// Serializes a collection of Grasshopper objects to a GhJsonDocument using a custom handler registry.
        /// </summary>
        /// <param name="objects">The objects to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <param name="handlerRegistry">Custom handler registry for component serialization.</param>
        /// <returns>A GhJsonDocument representing the serialized objects.</returns>
        public static GhJsonDocument Serialize(
            IEnumerable<IGH_ActiveObject> objects,
            SerializationOptions? options,
            ComponentHandlerRegistry handlerRegistry)
        {
            options ??= SerializationOptions.Standard;
            handlerRegistry ??= ComponentHandlerRegistry.Default;
            GeometricSerializerRegistry.Initialize();
            var objectList = objects.ToList();
            var document = new GhJsonDocument
            {
                SchemaVersion = "1.0",
                Components = new List<ComponentProperties>(),
                Connections = new List<ConnectionPairing>()
            };

            // Property-manager centric approach (legacy parity):
            // Build ComponentProperties from PropertyManagerV2 schema properties first,
            // then layer componentState + parameter settings.
            var ctx = GetSchemaPropertyContext(options);
            var propertyManager = new PropertyManagerV2(ctx);

            // Collect objects to serialize:
            // - all IGH_Component
            // - stand-alone IGH_Param only (skip params owned by a component)
            var objectsToSerialize = new List<IGH_ActiveObject>();
            objectsToSerialize.AddRange(objectList.OfType<IGH_Component>());
            objectsToSerialize.AddRange(objectList
                .OfType<IGH_Param>()
                .Where(p => p.Attributes?.Parent == null));

            // Build GUID to ID mapping from serialized objects only
            var guidToId = new Dictionary<Guid, int>();
            int nextId = 1;
            foreach (var obj in objectsToSerialize)
            {
                guidToId[obj.InstanceGuid] = nextId++;
            }

            foreach (var obj in objectsToSerialize)
            {
                var props = CreateComponentProperties(obj, guidToId, options, propertyManager, handlerRegistry);
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

        private static ComponentProperties? CreateComponentProperties(
            IGH_ActiveObject obj,
            Dictionary<Guid, int> guidToId,
            SerializationOptions options,
            PropertyManagerV2 propertyManager,
            ComponentHandlerRegistry handlerRegistry)
        {
            if (obj == null)
                return null;

            var pivot = obj.Attributes?.Pivot ?? new System.Drawing.PointF(0, 0);

            var props = new ComponentProperties
            {
                Name = obj.Name,
                NickName = !string.IsNullOrWhiteSpace(obj.NickName) && obj.NickName != obj.Name
                    ? obj.NickName
                    : null,
                ComponentGuid = obj.ComponentGuid,
                InstanceGuid = obj.InstanceGuid,
                Id = guidToId.TryGetValue(obj.InstanceGuid, out var id) ? id : 0,
                Pivot = new CompactPosition(pivot.X, pivot.Y)
            };

            // Schema properties
            if (options.IncludeSchemaProperties)
            {
                var schemaProps = propertyManager.ExtractProperties(obj);
                if (schemaProps.Count > 0)
                {
                    props.Properties = schemaProps;
                }
            }

            // Component state - use handler registry
            if (options.IncludeComponentState)
            {
                var handler = handlerRegistry.GetHandler(obj);
                props.ComponentState = handler.ExtractState(obj);

                // Move 'selected' to componentState per schema
                if (obj.Attributes?.Selected == true)
                {
                    props.ComponentState ??= new ComponentState();
                    props.ComponentState.Selected = true;
                }
            }

            // Avoid duplication: when a componentState.value exists, PersistentData becomes redundant and can be user-noise.
            if (props.ComponentState?.Value != null && props.Properties != null)
            {
                props.Properties.Remove("PersistentData");

                if (props.Properties.Count == 0)
                {
                    props.Properties = null;
                }
            }

            // Parameter settings
            if (options.IncludeParameterSettings && obj is IGH_Component component)
            {
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

            // Extract warnings and errors (only for Standard/Optimized, not Lite)
            if (options.IncludeWarningsAndErrors)
            {
                ExtractWarningsAndErrors(obj, props);
            }

            return props;
        }

        private static void ExtractWarningsAndErrors(IGH_ActiveObject obj, ComponentProperties props)
        {
            try
            {
                // Extract runtime messages from the component
                if (obj is IGH_Component component)
                {
                    var warnings = new List<string>();
                    var errors = new List<string>();

                    foreach (var msg in component.RuntimeMessages(GH_RuntimeMessageLevel.Warning))
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                            warnings.Add(msg);
                    }

                    foreach (var msg in component.RuntimeMessages(GH_RuntimeMessageLevel.Error))
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                            errors.Add(msg);
                    }

                    if (warnings.Count > 0)
                        props.Warnings = warnings;
                    if (errors.Count > 0)
                        props.Errors = errors;
                }
                else if (obj is IGH_Param param)
                {
                    var warnings = new List<string>();
                    var errors = new List<string>();

                    foreach (var msg in param.RuntimeMessages(GH_RuntimeMessageLevel.Warning))
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                            warnings.Add(msg);
                    }

                    foreach (var msg in param.RuntimeMessages(GH_RuntimeMessageLevel.Error))
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                            errors.Add(msg);
                    }

                    if (warnings.Count > 0)
                        props.Warnings = warnings;
                    if (errors.Count > 0)
                        props.Errors = errors;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GhJsonSerializer] Error extracting warnings/errors: {ex.Message}");
            }
        }

        private static void ExtractGroupInformation(
            GhJsonDocument document,
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

                int groupId = 1000; // Start group IDs at 1000 to avoid collision with component IDs
                foreach (var group in groups)
                {
                    var groupInfo = new GroupInfo
                    {
                        InstanceGuid = group.InstanceGuid,
                        Id = groupId++,
                        // Grasshopper groups use "Group" as the default title. We only want to
                        // serialize an explicit name when the user has actually set one.
                        Name = string.IsNullOrWhiteSpace(group.NickName) ||
                               string.Equals(group.NickName, "Group", StringComparison.Ordinal)
                            ? null
                            : group.NickName,
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
            // Backward-compatible entry point used by older call sites.
            // Prefer CreateComponentProperties (property-manager centric).
            var ctx = GetSchemaPropertyContext(options);
            var propertyManager = new PropertyManagerV2(ctx);
            return CreateComponentProperties(obj, guidToId, options, propertyManager, ComponentHandlerRegistry.Default);
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
            bool isScriptComponent = ScriptComponentFactory.IsScriptComponent(owner) || 
                                   ScriptComponentHelper.IsScriptComponentInstance(owner);

            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];

                // Skip the standard output "out" parameter for script components (including VB Script)
                // This is controlled by the UsingStandardOutputParam property in ComponentState
                if (isScriptComponent && !isInput && 
                    (string.Equals(p.Name, "out", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(p.NickName, "out", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

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
            var metadata = new DocumentMetadata
            {
                ComponentCount = objects.Count,
                GeneratorName = "GhJSON.Grasshopper",
                GeneratorVersion = GetAssemblyVersion(),
                Modified = DateTime.UtcNow.ToString("o") // ISO 8601 format
            };

            // Try to get Rhino/Grasshopper version info
            try
            {
                metadata.RhinoVersion = Rhino.RhinoApp.Version.ToString();
            }
            catch
            {
            }

            return metadata;
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                var assembly = typeof(GhJsonSerializer).Assembly;
                var version = assembly.GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}
