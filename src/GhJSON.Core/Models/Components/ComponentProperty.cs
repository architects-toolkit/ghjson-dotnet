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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents a property of a Grasshopper component.
    /// </summary>
    [JsonConverter(typeof(ComponentPropertyConverter))]
    public class ComponentProperty
    {
        /// <summary>
        /// Gets or sets the actual value of the property.
        /// </summary>
        [JsonProperty("value")]
        public object? Value { get; set; }
    }

    /// <summary>
    /// Custom JSON converter that serializes simple types (bool, int, string) directly
    /// without the {"value": ...} wrapper, while keeping the wrapper for complex types.
    /// </summary>
    public class ComponentPropertyConverter : JsonConverter<ComponentProperty>
    {
        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, ComponentProperty? value, JsonSerializer serializer)
        {
            if (value?.Value == null)
            {
                writer.WriteNull();
                return;
            }

            // For simple types (bool, int, string, double), write the value directly
            if (value.Value is bool ||
                value.Value is int ||
                value.Value is long ||
                value.Value is double ||
                value.Value is float ||
                value.Value is decimal ||
                value.Value is string)
            {
                serializer.Serialize(writer, value.Value);
            }
            else
            {
                // For complex types, keep the {"value": ...} wrapper
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                serializer.Serialize(writer, value.Value);
                writer.WriteEndObject();
            }
        }

        /// <inheritdoc/>
        public override ComponentProperty? ReadJson(JsonReader reader, Type objectType, ComponentProperty? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // If it's a simple value (not an object), read it directly
            if (reader.TokenType != JsonToken.StartObject)
            {
                var simpleValue = serializer.Deserialize(reader);
                return new ComponentProperty { Value = simpleValue };
            }

            // Otherwise, expect {"value": ...} structure
            var obj = serializer.Deserialize<JObject>(reader);
            if (obj != null && obj.TryGetValue("value", out var valueToken))
            {
                return new ComponentProperty { Value = valueToken.ToObject<object>() };
            }

            return new ComponentProperty();
        }
    }
}
