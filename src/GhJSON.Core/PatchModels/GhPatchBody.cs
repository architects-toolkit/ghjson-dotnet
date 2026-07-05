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

using Newtonsoft.Json;

namespace GhJSON.Core.PatchModels
{
    /// <summary>
    /// Container for all patch operations.
    /// </summary>
    public sealed class GhPatchBody
    {
        /// <summary>
        /// Gets or sets the reference to the base document this patch was generated against.
        /// </summary>
        [JsonProperty("base", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchBaseRef? Base { get; set; }

        /// <summary>
        /// Gets or sets the operations on document metadata.
        /// </summary>
        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchMetadataOp? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the operations on components.
        /// </summary>
        [JsonProperty("components", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchComponentsOp? Components { get; set; }

        /// <summary>
        /// Gets or sets the operations on connections.
        /// </summary>
        [JsonProperty("connections", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchConnectionsOp? Connections { get; set; }

        /// <summary>
        /// Gets or sets the operations on groups.
        /// </summary>
        [JsonProperty("groups", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchGroupsOp? Groups { get; set; }
    }

    /// <summary>
    /// Reference to the base document a patch was generated against.
    /// </summary>
    public sealed class GhPatchBaseRef
    {
        /// <summary>
        /// Gets or sets the schema version of the base document.
        /// </summary>
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public string? Schema { get; set; }

        /// <summary>
        /// Gets or sets the content checksum of the normalised base document.
        /// </summary>
        /// <remarks>
        /// Format: <c>&lt;algorithm&gt;-&lt;value&gt;</c>, e.g. <c>sha256-abc123...</c>.
        /// </remarks>
        [JsonProperty("checksum", NullValueHandling = NullValueHandling.Ignore)]
        public string? Checksum { get; set; }
    }
}
