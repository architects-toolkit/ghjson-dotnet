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
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace GhJSON.Grasshopper.Serialization.ComponentHandlers
{
    internal static class PersistentDataHandlerUtilities
    {
        internal static object? ExtractPersistentData(IGH_Param param)
        {
            IGH_Structure? dataTree = null;

            try
            {
                var persistentProp = param.GetType().GetProperty("PersistentData");
                if (persistentProp != null && typeof(IGH_Structure).IsAssignableFrom(persistentProp.PropertyType))
                {
                    dataTree = persistentProp.GetValue(param) as IGH_Structure;
                }
            }
            catch
            {
            }

            dataTree ??= param.VolatileData;

            if (dataTree == null)
                return null;

            var dictionary = SchemaProperties.DataTreeConverter.IGHStructureToDictionary(dataTree);
            return SchemaProperties.DataTreeConverter.IGHStructureDictionaryTo1DDictionary(dictionary);
        }

        internal static JArray FlattenPersistentDataToJArray(JObject persistentDataDict)
        {
            var values = new List<JToken>();

            foreach (var path in persistentDataDict)
            {
                if (path.Value is JObject pathData)
                {
                    foreach (var item in pathData)
                    {
                        if (item.Value is JObject itemData && itemData.ContainsKey("value"))
                        {
                            values.Add(itemData["value"]!);
                        }
                        else
                        {
                            values.Add(item.Value ?? JValue.CreateNull());
                        }
                    }
                }
            }

            return new JArray(values);
        }

        internal static void ApplyPersistentDataToParam<T>(Grasshopper.Kernel.GH_PersistentParam<T> param, object? persistentData, Func<JToken, T> converter)
            where T : class, IGH_Goo
        {
            if (param == null || persistentData == null)
                return;

            if (persistentData is JToken jt)
            {
                if (jt.Type == JTokenType.Object)
                {
                    ApplyPersistentDataToParam(param, (JObject)jt, converter);
                }

                return;
            }

            if (persistentData is JObject jo)
            {
                ApplyPersistentDataToParam(param, jo, converter);
            }
        }

        internal static void ApplyPersistentDataToParam<T>(Grasshopper.Kernel.GH_PersistentParam<T> param, JObject persistentDataDict, Func<JToken, T> converter)
            where T : class, IGH_Goo
        {
            var arrayData = FlattenPersistentDataToJArray(persistentDataDict);
            var pData = SchemaProperties.DataTreeConverter.JObjectToIGHStructure(arrayData, converter);
            param.SetPersistentData(pData);
        }

    }
}
