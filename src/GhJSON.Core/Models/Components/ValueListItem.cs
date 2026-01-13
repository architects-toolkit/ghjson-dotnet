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

namespace GhJSON.Core.Models.Components
{
    /// <summary>
    /// Represents an item in a GH_ValueList component.
    /// </summary>
    public class ValueListItem
    {
        /// <summary>
        /// Gets or sets the display name of the item.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expression/value of the item.
        /// </summary>
        [JsonProperty("expression")]
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this item is selected.
        /// Only serialized if true to reduce JSON size.
        /// </summary>
        [JsonProperty("selected", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Selected { get; set; }
    }
}
