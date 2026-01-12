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
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Document
{
    /// <summary>
    /// Represents a Grasshopper group with its members and properties.
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for this group instance.
        /// Optional when using integer ID.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the integer ID of the group.
        /// Must be unique within the file. Used for compact representation.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the name/nickname of the group.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the group color in ARGB format (e.g., "255,0,200,0").
        /// </summary>
        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the list of component IDs that belong to this group.
        /// Uses integer IDs instead of GUIDs for compact representation.
        /// </summary>
        [JsonProperty("members")]
        public List<int> Members { get; set; } = new List<int>();
    }
}
