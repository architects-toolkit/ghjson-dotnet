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
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// JSON converter for <see cref="GhJsonPivot"/> that supports both compact string format
    /// and object format as defined in the schema.
    /// </summary>
    internal sealed class PivotConverter : JsonConverter<GhJsonPivot>
    {
        /// <inheritdoc/>
        public override GhJsonPivot? ReadJson(
            JsonReader reader,
            Type objectType,
            GhJsonPivot? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                var compact = reader.Value?.ToString();
                return GhJsonPivot.FromCompact(compact ?? string.Empty);
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                var x = obj["x"]?.Value<double>() ?? 0;
                var y = obj["y"]?.Value<double>() ?? 0;
                return new GhJsonPivot(x, y);
            }

            throw new JsonSerializationException(
                $"Unexpected token type {reader.TokenType} when parsing pivot");
        }

        /// <inheritdoc/>
        public override void WriteJson(
            JsonWriter writer,
            GhJsonPivot? value,
            JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Always write in compact format for optimization
            writer.WriteValue(value.ToCompact());
        }
    }
}
