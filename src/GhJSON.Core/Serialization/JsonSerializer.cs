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

using GhJSON.Core.SchemaModels;
using Newtonsoft.Json;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// Internal JSON serialization utilities for GhJSON documents.
    /// </summary>
    internal static class GhJsonSerializer
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = { new PivotConverter() }
        };

        /// <summary>
        /// Serializes a GhJSON document to a JSON string.
        /// </summary>
        /// <param name="document">The document to serialize.</param>
        /// <param name="options">Optional write options.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string Serialize(GhJsonDocument document, WriteOptions? options = null)
        {
            options ??= WriteOptions.Default;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = options.IncludeNullValues
                    ? NullValueHandling.Include
                    : NullValueHandling.Ignore,
                Formatting = options.Indented ? Formatting.Indented : Formatting.None,
                Converters = { new PivotConverter() }
            };

            return JsonConvert.SerializeObject(document, settings);
        }

        /// <summary>
        /// Deserializes a JSON string to a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized GhJSON document.</returns>
        public static GhJsonDocument Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<GhJsonDocument>(json, DefaultSettings)
                ?? new GhJsonDocument();
        }
    }
}
