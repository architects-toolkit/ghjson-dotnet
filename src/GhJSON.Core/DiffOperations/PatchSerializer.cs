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

using GhJSON.Core.PatchModels;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// JSON serialization for <see cref="GhPatchDocument"/>.
    /// </summary>
    internal static class PatchSerializer
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = { new PivotConverter() },
        };

        public static string Serialize(GhPatchDocument patch, WriteOptions? options = null)
        {
            options ??= WriteOptions.Default;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = options.IncludeNullValues
                    ? NullValueHandling.Include
                    : NullValueHandling.Ignore,
                Formatting = options.Indented ? Formatting.Indented : Formatting.None,
                Converters = { new PivotConverter() },
            };

            return JsonConvert.SerializeObject(patch, settings);
        }

        public static GhPatchDocument Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<GhPatchDocument>(json, DefaultSettings)
                ?? new GhPatchDocument();
        }
    }
}
