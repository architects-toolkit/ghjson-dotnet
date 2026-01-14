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

using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents an endpoint of a connection, referencing a component parameter.
    /// Maps to the connectionEndpoint definition in the schema.
    /// </summary>
    public sealed class GhJsonConnectionEndpoint
    {
        /// <summary>
        /// Gets or sets the integer ID of the component.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter on the component.
        /// </summary>
        [JsonProperty("paramName", NullValueHandling = NullValueHandling.Ignore)]
        public string? ParamName { get; set; }

        /// <summary>
        /// Gets or sets the zero-based index of the parameter.
        /// Used for reliable matching regardless of display name settings.
        /// </summary>
        [JsonProperty("paramIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int? ParamIndex { get; set; }
    }
}
