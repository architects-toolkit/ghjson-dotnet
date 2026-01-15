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
using System.Globalization;
using System.Linq;
using System.Reflection;
using GhJSON.Core.SchemaModels;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GhJSON.Grasshopper.Serialization.ObjectHandlers
{
    /// <summary>
    /// Handler for internalized (persistent) data in parameters.
    /// </summary>
    internal sealed class InternalizedDataHandler : IObjectHandler
    {
        /// <inheritdoc/>
        public int Priority => 0;

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
            if (obj is IGH_Component comp)
            {
                SerializeInternalizedData(comp.Params.Input, component.InputSettings);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    SerializeParamData(param, component.OutputSettings[0]);
                }
            }
        }

        /// <inheritdoc/>
        public void Deserialize(GhJsonComponent component, IGH_DocumentObject obj)
        {
            if (obj is IGH_Component comp)
            {
                DeserializeInternalizedData(component.InputSettings, comp.Params.Input);
            }
            else if (obj is IGH_Param param)
            {
                if (component.OutputSettings?.Count > 0)
                {
                    DeserializeParamData(component.OutputSettings[0], param);
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

            var setPersistentDataMethod = param.GetType().GetMethod("SetPersistentData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

            if (openParen < 0 || closeParen <= openParen)
            {
                return int.MaxValue;
            }

            var indexStr = itemKey.Substring(openParen + 1, closeParen - openParen - 1);
            return int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                ? idx
                : int.MaxValue;
        }

        private static GH_Path ParsePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new GH_Path(0);
            }

            var trimmed = path.Trim();

            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return new GH_Path(0);
            }

            var parts = trimmed.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var indices = parts
                .Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture))
                .ToArray();

            return new GH_Path(indices);
        }

        private static object CreateGoo(Type gooType, object? value)
        {
            if (typeof(IGH_Goo).IsAssignableFrom(gooType) && value != null && gooType.IsInstanceOfType(value))
            {
                return value;
            }

            var gooObj = Activator.CreateInstance(gooType);
            if (gooObj is not IGH_Goo goo)
            {
                throw new InvalidOperationException($"Failed to create IGH_Goo instance of type {gooType.FullName}");
            }

            if (value == null)
            {
                return goo;
            }

            var castOk = goo.CastFrom(value);
            if (!castOk)
            {
                throw new InvalidOperationException($"Failed to cast value '{value}' ({value.GetType().FullName}) to goo type {gooType.FullName}");
            }

            return goo;
        }

        private static void SerializeInternalizedData(
            IList<IGH_Param> parameters,
            List<GhJsonParameterSettings>? settings)
        {
            if (parameters == null || settings == null)
            {
                return;
            }

            for (var i = 0; i < parameters.Count && i < settings.Count; i++)
            {
                SerializeParamData(parameters[i], settings[i]);
            }
        }

        private static void SerializeParamData(IGH_Param param, GhJsonParameterSettings settings)
        {
            if (param.DataType != GH_ParamData.local)
            {
                return; // No persistent data
            }

            var persistentData = param.VolatileData;
            if (persistentData.IsEmpty)
            {
                return;
            }

            var dataTree = new Dictionary<string, Dictionary<string, string>>();

            foreach (var path in persistentData.Paths)
            {
                var branch = persistentData.get_Branch(path);
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
                    var serialized = DataTypeRegistry.Serialize(goo.ScriptVariable());

                    if (!string.IsNullOrEmpty(serialized))
                    {
                        branchData[itemKey] = serialized;
                    }
                    else
                    {
                        // Fallback to string representation
                        branchData[itemKey] = $"text:{goo}";
                    }
                }

                if (branchData.Count > 0)
                {
                    dataTree[pathKey] = branchData;
                }
            }

            if (dataTree.Count > 0)
            {
                settings.InternalizedData = dataTree;
            }
        }
    }
}
