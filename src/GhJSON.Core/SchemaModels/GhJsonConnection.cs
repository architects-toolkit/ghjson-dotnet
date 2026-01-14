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
    /// Represents a connection (wire) between two component parameters.
    /// Maps to the connectionData definition in the schema.
    /// </summary>
    public sealed class GhJsonConnection
    {
        /// <summary>
        /// Gets or sets the source endpoint (output parameter).
        /// </summary>
        [JsonProperty("from")]
        public GhJsonConnectionEndpoint From { get; set; } = new GhJsonConnectionEndpoint();

        /// <summary>
        /// Gets or sets the target endpoint (input parameter).
        /// </summary>
        [JsonProperty("to")]
        public GhJsonConnectionEndpoint To { get; set; } = new GhJsonConnectionEndpoint();
    }
}
