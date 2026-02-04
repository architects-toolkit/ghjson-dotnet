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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents UI-specific state for components.
    /// Maps to the componentState definition in the schema.
    /// The properties used depend on the component type.
    /// </summary>
    public sealed class GhJsonComponentState
    {
        /// <summary>
        /// Gets or sets a value indicating whether the component is currently selected on the canvas.
        /// </summary>
        [JsonProperty("selected", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Selected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component is locked (disabled).
        /// </summary>
        [JsonProperty("locked", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Locked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component preview is hidden.
        /// </summary>
        [JsonProperty("hidden", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets extension data produced by object handlers.
        /// Keys are extension identifiers, values are extension-specific objects.
        /// </summary>
        [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(GhJSON.Core.Serialization.ExtensionsDictionaryConverter))]
        public Dictionary<string, object>? Extensions { get; set; }

        /// <summary>
        /// Gets or sets additional properties that don't fit into standard fields.
        /// This allows forward compatibility with future schema versions.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
