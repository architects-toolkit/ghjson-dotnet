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
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// JSON converter for <see cref="GhJsonPivot"/> that supports the compact string format,
    /// the object format, and the [x,y] array format defined in the schema.
    /// </summary>
    public sealed class PivotConverter : JsonConverter<GhJsonPivot>
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
                var xToken = obj["x"];
                var yToken = obj["y"];
                if ((xToken?.Type != JTokenType.Integer && xToken?.Type != JTokenType.Float) ||
                    (yToken?.Type != JTokenType.Integer && yToken?.Type != JTokenType.Float))
                {
                    throw new JsonSerializationException(
                        "Pivot object properties x and y must be numeric.");
                }

                return new GhJsonPivot(
                    (int)Math.Round(xToken.Value<double>()),
                    (int)Math.Round(yToken.Value<double>()));
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var array = JArray.Load(reader);
                if (array.Count != 2 ||
                    (array[0].Type != JTokenType.Integer && array[0].Type != JTokenType.Float) ||
                    (array[1].Type != JTokenType.Integer && array[1].Type != JTokenType.Float))
                {
                    throw new JsonSerializationException(
                        "Pivot array must contain exactly two numeric coordinates: [x, y].");
                }

                return new GhJsonPivot(
                    (int)Math.Round(array[0].Value<double>()),
                    (int)Math.Round(array[1].Value<double>()));
            }

            throw new JsonSerializationException(
                $"Unexpected token type {reader.TokenType} when parsing pivot. " +
                "Expected null, \"x,y\" string, {x,y} object, or [x,y] array.");
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

            // Emit the compact integer string form. The v1.0 schema accepts both the compact
            // "X,Y" format and the object {x,y} format as long as coordinates are integers.
            writer.WriteValue(value.ToCompact());
        }
    }
}
