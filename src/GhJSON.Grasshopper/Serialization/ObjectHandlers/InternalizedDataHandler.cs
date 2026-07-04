/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using GhJSON.Grasshopper.Shared;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for internalized (persistent) and runtime (volatile) data in parameters.
    /// </summary>
    internal sealed class InternalizedDataHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 1000;

        /// <inheritdoc/>
        public string? SchemaExtensionUrl => null;

        /// <inheritdoc/>
        public bool CanHandle(IGH_DocumentObject obj)
        {
            return obj is IGH_Component || obj is IGH_Param;
        }

        /// <inheritdoc/>
        public bool CanHandle(GhJsonComponent component)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Serialize(IGH_DocumentObject obj, GhJsonComponent component)
        {
#if DEBUG
            Debug.WriteLine($"[InternalizedDataHandler.Serialize] Serializing data for: {obj?.Name}, ObjType: {obj?.GetType().Name}");
#endif

            var options = ObjectHandlerOrchestrator.CurrentOptions;
            bool includeInternalized = options?.IncludeInternalizedData ?? true;
            bool includeRuntime = options?.IncludeRuntimeData ?? false;

            if (!includeInternalized && !includeRuntime)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.Serialize] SKIPPED {obj?.Name}: both IncludeInternalizedData and IncludeRuntimeData are false");
#endif
                return;
            }

            if (obj is IGH_Component comp)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.Serialize] Object is IGH_Component, InputCount: {comp.Params.Input.Count}, OutputCount: {comp.Params.Output.Count}, InputSettingsCount: {component.InputSettings?.Count}, OutputSettingsCount: {component.OutputSettings?.Count}");
#endif
                if (includeInternalized)
                {
                    SerializeInternalizedData(comp.Params.Input, component.InputSettings);
                }

                if (includeRuntime)
                {
                    SerializeRuntimeData(comp.Params.Output, component.OutputSettings);
                }
            }
            else if (obj is IGH_Param param)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.Serialize] Object is IGH_Param, DataType: {param.DataType}");
#endif

                // List is guaranteed to exist by orchestrator - find or create settings for this param
                var settings = component.OutputSettings.FirstOrDefault(s => s.ParameterName == param.Name);
                if (settings == null)
                {
                    settings = new GhJsonParameterSettings { ParameterName = param.Name };
                    component.OutputSettings.Add(settings);
                }

                if (includeInternalized)
                {
                    SerializeParamData(param, settings);
                }

                if (includeRuntime)
                {
                    SerializeParamRuntimeData(param, settings);
                }
            }
            else
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.Serialize] SKIPPED: Object is neither IGH_Component nor IGH_Param");
#endif
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
#if DEBUG
            Debug.WriteLine($"[InternalizedDataHandler.Deserialize] Deserializing internalized data for: {obj?.Name}");
#endif

            if (obj is IGH_Component comp)
            {
                DeserializeInternalizedData(component.InputSettings, comp.Params.Input);
            }
            else if (obj is IGH_Param param)
            {
                var settings = component.OutputSettings?.FirstOrDefault(s => s.ParameterName == param.Name);
                if (settings != null)
                {
                    DeserializeParamData(settings, param);
                }
            }
        }

        private static void DeserializeInternalizedData(
            List<GhJsonParameterSettings>? settings,
            IList<IGH_Param> parameters)
        {
            if (settings == null || parameters == null)
            {
                return;
            }

            foreach (var paramSettings in settings)
            {
                var param = FindParameter(parameters, paramSettings.ParameterName);
                if (param != null)
                {
                    DeserializeParamData(paramSettings, param);
                }
            }
        }

        private static IGH_Param? FindParameter(IList<IGH_Param> parameters, string name)
        {
            foreach (var param in parameters)
            {
                if (param.Name == name)
                {
                    return param;
                }
            }

            return null;
        }

        private static void DeserializeParamData(GhJsonParameterSettings settings, IGH_Param param)
        {
            if (settings.InternalizedData == null || settings.InternalizedData.Count == 0)
            {
                return;
            }

            var persistentParamBaseType = FindGenericBaseType(param.GetType(), typeof(global::Grasshopper.Kernel.GH_PersistentParam<>));
            if (persistentParamBaseType == null)
            {
                return;
            }

            var gooType = persistentParamBaseType.GetGenericArguments()[0];
            var structureType = typeof(GH_Structure<>).MakeGenericType(gooType);
            var structure = Activator.CreateInstance(structureType);
            if (structure == null)
            {
                return;
            }

            var appendMethod = structureType.GetMethod("Append", new[] { gooType, typeof(GH_Path) });
            if (appendMethod == null)
            {
                return;
            }

            foreach (var pathEntry in settings.InternalizedData)
            {
                var path = ParsePath(pathEntry.Key);
                foreach (var item in OrderItemsByIndex(pathEntry.Value))
                {
                    var deserialized = DataTypeRegistry.Deserialize(item.Value);
                    var goo = CreateGoo(gooType, deserialized);
                    appendMethod.Invoke(structure, new object?[] { goo, path });
                }
            }

            var setPersistentDataMethod = param.GetType().GetMethod(
                "SetPersistentData",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { structureType },
                null);
            if (setPersistentDataMethod == null)
            {
                return;
            }

            setPersistentDataMethod.Invoke(param, new[] { structure });
        }

        private static Type? FindGenericBaseType(Type type, Type openGenericBaseType)
        {
            var current = type;
            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseType)
                {
                    return current;
                }

                current = current.BaseType;
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> OrderItemsByIndex(Dictionary<string, string> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            return items.OrderBy(kv => ExtractItemIndex(kv.Key));
        }

        private static int ExtractItemIndex(string itemKey)
        {
            if (string.IsNullOrWhiteSpace(itemKey))
            {
                return int.MaxValue;
            }

            var openParen = itemKey.LastIndexOf('(');
            var closeParen = itemKey.LastIndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                var indexStr = itemKey.Substring(openParen + 1, closeParen - openParen - 1);
                if (int.TryParse(indexStr, out var index))
                {
                    return index;
                }
            }

            return int.MaxValue;
        }

        private static GH_Path ParsePath(string pathString)
        {
            if (string.IsNullOrWhiteSpace(pathString))
            {
                return new GH_Path(0);
            }

            // Strip parentheses if present: "{0;1}" -> "0;1"
            var clean = pathString.Trim('{', '}', ' ', '\t');
            if (string.IsNullOrWhiteSpace(clean))
            {
                return new GH_Path(0);
            }

            var parts = clean.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var indices = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                {
                    indices.Add(index);
                }
            }

            return indices.Count > 0 ? new GH_Path(indices.ToArray()) : new GH_Path(0);
        }

        private static object? CreateGoo(Type gooType, object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                // Try the (object) constructor first
                var constructor = gooType.GetConstructor(new[] { value.GetType() });
                if (constructor != null)
                {
                    return constructor.Invoke(new[] { value });
                }

                // Try a string constructor for text values
                if (value is string strValue)
                {
                    constructor = gooType.GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                    {
                        return constructor.Invoke(new object?[] { strValue });
                    }
                }

                // Try parameterless constructor and set Value property
                constructor = gooType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var goo = constructor.Invoke(Array.Empty<object>());
                    var valueProperty = gooType.GetProperty("Value");
                    valueProperty?.SetValue(goo, value);
                    return goo;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InternalizedDataHandler.CreateGoo] Error creating {gooType.Name}: {ex.Message}");
            }

            return null;
        }

        private static void SerializeInternalizedData(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings> settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            // IOIdentificationHandler creates settings in parameter order
            // Simple index matching is sufficient
            int count = Math.Min(parameters.Count, settings.Count);
            for (int i = 0; i < count; i++)
            {
                SerializeParamData(parameters[i], settings[i]);
            }
        }

        private static void SerializeRuntimeData(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings> settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            int count = Math.Min(parameters.Count, settings.Count);
            for (int i = 0; i < count; i++)
            {
                SerializeParamRuntimeData(parameters[i], settings[i]);
            }
        }

        private static void SerializeParamData(IGH_Param param, GhJsonParameterSettings settings)
        {
#if DEBUG
            Debug.WriteLine($"[InternalizedDataHandler.SerializeParamData] Checking param: {param.Name}, DataType: {param.DataType}");
#endif

            if (param.DataType != GH_ParamData.local)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.SerializeParamData] SKIPPED {param.Name}: DataType is {param.DataType} (not local)");
#endif
                return; // No persistent data
            }

            var persistentData = GetPersistentDataStructure(param);
            if (persistentData == null)
            {
                return;
            }

            var dataTree = SerializeStructureData(persistentData);
            if (dataTree.Count > 0)
            {
                settings.InternalizedData = dataTree;
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.SerializeParamData] SUCCESS: Serialized {dataTree.Count} paths for {param.Name}");
#endif
            }
        }

        private static void SerializeParamRuntimeData(IGH_Param param, GhJsonParameterSettings settings)
        {
#if DEBUG
            Debug.WriteLine($"[InternalizedDataHandler.SerializeParamRuntimeData] Checking param: {param.Name}");
#endif

            try
            {
                var volatileData = param.VolatileData as IGH_Structure;
                if (volatileData == null || volatileData.IsEmpty)
                {
#if DEBUG
                    Debug.WriteLine($"[InternalizedDataHandler.SerializeParamRuntimeData] SKIPPED {param.Name}: volatile data is null or empty");
#endif
                    return;
                }

                var dataTree = SerializeStructureData(volatileData);
                if (dataTree.Count > 0)
                {
                    settings.RuntimeData = dataTree;
#if DEBUG
                    Debug.WriteLine($"[InternalizedDataHandler.SerializeParamRuntimeData] SUCCESS: Serialized {dataTree.Count} paths for {param.Name}");
#endif
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InternalizedDataHandler.SerializeParamRuntimeData] Error reading volatile data for {param.Name}: {ex.Message}");
            }
        }

        private static IGH_Structure? GetPersistentDataStructure(IGH_Param param)
        {
            // Access PersistentData via reflection (it's on GH_PersistentParam<T>, not IGH_Param)
            var persistentDataProperty = ReflectionCache.GetProperty(param.GetType(), "PersistentData");
            if (persistentDataProperty == null)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.GetPersistentDataStructure] SKIPPED {param.Name}: PersistentData property not found on {param.GetType().FullName}");
#endif
                return null;
            }

            var persistentData = persistentDataProperty.GetValue(param) as IGH_Structure;
            if (persistentData == null)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.GetPersistentDataStructure] SKIPPED {param.Name}: persistentData is null");
#endif
                return null;
            }

            if (persistentData.IsEmpty)
            {
#if DEBUG
                Debug.WriteLine($"[InternalizedDataHandler.GetPersistentDataStructure] SKIPPED {param.Name}: persistentData is empty");
#endif
                return null;
            }

            return persistentData;
        }

        private static Dictionary<string, Dictionary<string, string>> SerializeStructureData(IGH_Structure structure)
        {
            var dataTree = new Dictionary<string, Dictionary<string, string>>();

            foreach (var path in structure.Paths)
            {
                var branch = structure.get_Branch(path);
                if (branch == null || branch.Count == 0)
                {
                    continue;
                }

                var pathKey = path.ToString();
                var branchData = new Dictionary<string, string>();

                for (var i = 0; i < branch.Count; i++)
                {
                    var goo = branch[i] as IGH_Goo;
                    if (goo == null)
                    {
                        continue;
                    }

                    var itemKey = $"{pathKey}({i})";
                    var scriptVar = goo.ScriptVariable();
                    var serialized = DataTypeRegistry.Serialize(scriptVar);

                    if (!string.IsNullOrEmpty(serialized))
                    {
                        // Skip empty text values (e.g., "text:" with no content)
                        if (serialized.StartsWith("text:", StringComparison.OrdinalIgnoreCase) &&
                            serialized.Length == "text:".Length)
                        {
                            continue;
                        }

                        branchData[itemKey] = serialized;
                    }
                    else
                    {
                        // Fallback to string representation
                        var fallback = $"text:{goo}";
                        if (fallback.Length > "text:".Length)
                        {
                            branchData[itemKey] = fallback;
                        }
                    }
                }

                if (branchData.Count > 0)
                {
                    dataTree[pathKey] = branchData;
                }
            }

            return dataTree;
        }
    }
}
