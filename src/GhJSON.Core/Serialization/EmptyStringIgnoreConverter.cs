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

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// JSON converter that treats empty strings as null for serialization.
    /// Use with NullValueHandling.Ignore to omit empty strings from JSON output.
    /// </summary>
    public class EmptyStringIgnoreConverter : JsonConverter<string>
    {
        /// <summary>
        /// Writes the JSON representation of the string value.
        /// Treats empty strings as null (to be omitted with NullValueHandling.Ignore).
        /// </summary>
        public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value);
        }

        /// <summary>
        /// Reads the JSON representation of the string value.
        /// </summary>
        public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                return (string?)reader.Value;
            }

            return null;
        }
    }
}
