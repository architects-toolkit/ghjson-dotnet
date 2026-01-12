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

namespace GhJSON.Core.Models.Connections
{
    /// <summary>
    /// Represents a connection endpoint in a Grasshopper document.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Gets or sets the integer ID of the component.
        /// </summary>
        [JsonProperty("id", Order = 1)]
        [JsonRequired]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter on the component.
        /// Optional when using paramIndex.
        /// </summary>
        [JsonProperty("paramName", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
        public string? ParamName { get; set; }

        /// <summary>
        /// Gets or sets the zero-based index of the parameter.
        /// Used for reliable parameter matching regardless of display name settings.
        /// </summary>
        [JsonProperty("paramIndex", Order = 3)]
        public int? ParamIndex { get; set; }

        /// <summary>
        /// Checks if the connection has valid component ID and either parameter name or index.
        /// </summary>
        /// <returns>True if the connection has a valid ID and either parameter name or index.</returns>
        public bool IsValid()
        {
            return this.Id > 0 && (!string.IsNullOrEmpty(this.ParamName) || this.ParamIndex.HasValue);
        }
    }
}
