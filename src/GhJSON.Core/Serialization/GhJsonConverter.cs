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
using System.IO;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.Serialization
{
    /// <summary>
    /// Utility class for serializing and deserializing GhJSON documents to/from JSON format.
    /// </summary>
    public static class GhJsonConverter
    {
        /// <summary>
        /// Default JSON serialization settings with formatting.
        /// </summary>
        private static readonly JsonSerializerSettings DefaultSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new CompactPositionConverter() }
        };

        /// <summary>
        /// Serialize a GhJSON document to JSON string.
        /// </summary>
        /// <param name="document">The GhJSON document to serialize.</param>
        /// <param name="settings">Optional JSON serializer settings.</param>
        /// <returns>A JSON string representation of the document.</returns>
        public static string SerializeToJson(GhJsonDocument document, JsonSerializerSettings? settings = null)
        {
            return JsonConvert.SerializeObject(document, settings ?? DefaultSettings);
        }

        /// <summary>
        /// Deserialize a JSON string to a GhJSON document.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="fixJson">Whether to apply fixes before deserialization.</param>
        /// <param name="settings">Optional JSON serializer settings.</param>
        /// <returns>A GhJSON document object.</returns>
        public static GhJsonDocument? DeserializeFromJson(
            string json,
            bool fixJson = true,
            JsonSerializerSettings? settings = null)
        {
            var jroot = JObject.Parse(json);

            if (fixJson)
            {
                var (fixedJson, _) = GhJsonFixer.FixAll(jroot);
                jroot = fixedJson;
            }

            // Deserialize into document
            return JsonConvert.DeserializeObject<GhJsonDocument>(jroot.ToString(), settings ?? DefaultSettings);
        }

        /// <summary>
        /// Save a GhJSON document to a JSON file.
        /// </summary>
        /// <param name="document">The GhJSON document to save.</param>
        /// <param name="filePath">The file path to save to.</param>
        /// <param name="settings">Optional JSON serializer settings.</param>
        public static void SaveToFile(GhJsonDocument document, string filePath, JsonSerializerSettings? settings = null)
        {
            string json = SerializeToJson(document, settings);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Load a GhJSON document from a JSON file.
        /// </summary>
        /// <param name="filePath">The file path to load from.</param>
        /// <param name="fixJson">Whether to apply fixes before deserialization.</param>
        /// <param name="settings">Optional JSON serializer settings.</param>
        /// <returns>A GhJSON document object.</returns>
        public static GhJsonDocument? LoadFromFile(
            string filePath,
            bool fixJson = true,
            JsonSerializerSettings? settings = null)
        {
            string json = File.ReadAllText(filePath);
            return DeserializeFromJson(json, fixJson, settings);
        }
    }
}
