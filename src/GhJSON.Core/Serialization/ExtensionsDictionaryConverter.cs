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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// Custom JSON converter that ensures the Extensions dictionary always contains
    /// Dictionary<string, object> values instead of JObject instances during deserialization.
    /// This simplifies handler code by eliminating the need to check for both types.
    /// </summary>
    internal sealed class ExtensionsDictionaryConverter : JsonConverter<Dictionary<string, object>?>
    {
        /// <inheritdoc/>
        public override Dictionary<string, object>? ReadJson(
            JsonReader reader,
            Type objectType,
            Dictionary<string, object>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var result = new Dictionary<string, object>();

            foreach (var property in jObject.Properties())
            {
                result[property.Name] = ConvertJTokenToObject(property.Value);
            }

            return result;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Dictionary<string, object>? value, JsonSerializer serializer)
        {
            // Use default serialization for writing
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }

        /// <summary>
        /// Recursively converts JToken instances to their equivalent CLR objects.
        /// This ensures JObject becomes Dictionary&lt;string, object&gt; and JArray becomes List&lt;object&gt;.
        /// </summary>
        private static object ConvertJTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jObj = (JObject)token;
                    var dict = new Dictionary<string, object>();
                    foreach (var property in jObj.Properties())
                    {
                        dict[property.Name] = ConvertJTokenToObject(property.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    var jArr = (JArray)token;
                    var list = new List<object>();
                    foreach (var item in jArr)
                    {
                        list.Add(ConvertJTokenToObject(item));
                    }
                    return list;

                case JTokenType.Integer:
                    // Try to preserve numeric types appropriately
                    var longValue = token.Value<long>();
                    if (longValue >= int.MinValue && longValue <= int.MaxValue)
                    {
                        return (int)longValue;
                    }
                    return longValue;

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.String:
                    return token.Value<string>() ?? string.Empty;

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Date:
                    return token.Value<DateTime>();

                case JTokenType.Null:
                    return null!;

                default:
                    // For other types, convert to object
                    return token.ToObject<object>() ?? new object();
            }
        }
    }
}
