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
using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents a Grasshopper group containing multiple components.
    /// Maps to the groupData definition in the schema.
    /// </summary>
    public sealed class GhJsonGroup
    {
        /// <summary>
        /// Gets or sets the unique identifier for this group instance.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the integer ID of the group.
        /// Must be unique within the file.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the group color in ARGB format.
        /// </summary>
        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets list of component integer IDs that belong to this group.
        /// </summary>
        [JsonProperty("members")]
        public List<int> Members { get; set; } = new List<int>();
    }
}
