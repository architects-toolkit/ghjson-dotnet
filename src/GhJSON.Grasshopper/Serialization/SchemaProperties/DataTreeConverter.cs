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
using System.Text.RegularExpressions;
using GhJSON.Core.Serialization.DataTypes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization.SchemaProperties
{
    /// <summary>
    /// Utility methods for converting Grasshopper data trees to JSON-friendly structures and back.
    /// </summary>
    public static partial class DataTreeConverter
    {
        [GeneratedRegex(@"\(\d+\)")]
        private static partial Regex ListIndicesRegex();

        public static Dictionary<string, List<object?>> IGHStructureToDictionary(IGH_Structure structure)
        {
            Dictionary<string, List<object?>> result = new();

            foreach (GH_Path path in structure.Paths)
            {
                List<object?> dataList = new();

                foreach (object dataItem in structure.get_Branch(path))
                {
                    object? serializedItem = SerializeDataItem(dataItem);
                    dataList.Add(serializedItem);
                }

                result.Add(path.ToString(), dataList);
            }

            return result;
        }

        private static object? SerializeDataItem(object dataItem)
        {
            if (dataItem == null)
            {
                return null;
            }

            object actualValue = dataItem;
            if (dataItem is IGH_Goo goo)
            {
                actualValue = goo.ScriptVariable();
            }

            if (actualValue == null)
            {
                return null;
            }

            Type valueType = actualValue.GetType();
            if (DataTypeSerializer.IsTypeSupported(valueType))
            {
                try
                {
                    string? serializedValue = DataTypeSerializer.Serialize(actualValue);
                    return serializedValue ?? actualValue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataTreeConverter] Error serializing {valueType.Name}: {ex.Message}");
                    return actualValue;
                }
            }

            return actualValue;
        }

        public static Dictionary<string, object> IGHStructureDictionaryTo1DDictionary(Dictionary<string, List<object?>> dictionary)
        {
            Dictionary<string, object> result = new();

            foreach (var kvp in dictionary)
            {
                if (kvp.Value is List<object?> list)
                {
                    var tempDict = new Dictionary<string, object?>();
                    int index = 0;
                    foreach (var val in list)
                    {
                        tempDict.Add($"{kvp.Key}({index++})", val);
                    }

                    result.Add(kvp.Key, tempDict);
                }
                else
                {
                    result.Add(kvp.Key, new { kvp.Key, kvp.Value });
                }
            }

            return result;
        }

        public static GH_Structure<T> JObjectToIGHStructure<T>(JToken input, Func<JToken, T> convertFunction)
            where T : IGH_Goo
        {
            GH_Structure<T> result = new();

            if (input is JArray array)
            {
                var defaultPath = new GH_Path(0);
                foreach (var value in array)
                {
                    result.Append(convertFunction(value), defaultPath);
                }

                return result;
            }

            if (input is JObject jObject)
            {
                foreach (var path in jObject)
                {
                    GH_Path p = new(ParseKeyToPath(path.Key));
                    if (path.Value is not JObject items)
                    {
                        continue;
                    }

                    foreach (var item in items)
                    {
                        if (item.Value is not JObject itemObj)
                        {
                            continue;
                        }

                        foreach (var property in itemObj.Properties())
                        {
                            if (property.Name == "Value" || property.Name == "value")
                            {
                                JToken valueToken = property.Value;
                                string valueString = valueToken.ToString();

                                if (DataTypeSerializer.TryDeserializeFromPrefix(valueString, out object? deserializedValue) && deserializedValue != null)
                                {
                                    valueToken = JToken.FromObject(deserializedValue);
                                    Debug.WriteLine($"{p} deserialized from inline format: {valueString}");
                                }

                                result.Append(convertFunction(valueToken), p);
                                Debug.WriteLine($"{p} value found to be: {valueToken}");
                            }
                        }
                    }
                }

                return result;
            }

            result.Append(convertFunction(input), new GH_Path(0));
            return result;
        }

        private static GH_Path ParseKeyToPath(string key)
        {
            string cleanedKey = ListIndicesRegex().Replace(key, string.Empty);
            var pathElements = cleanedKey.Trim('{', '}').Split(';');

            List<int> indices = new();
            foreach (var element in pathElements)
            {
                if (int.TryParse(element, out int index))
                {
                    indices.Add(index);
                }
            }

            return new GH_Path(indices.ToArray());
        }
    }
}
