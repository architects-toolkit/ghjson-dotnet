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
    /// Represents a GhPatch document — a sibling format to GhJSON describing
    /// add/remove/modify operations on components, connections, groups, and metadata.
    /// </summary>
    /// <remarks>
    /// See <see href="https://architects-toolkit.github.io/ghjson-spec/docs/ghpatch.html"/>
    /// for the format specification.
    /// </remarks>
    public sealed class GhPatchDocument
    {
        /// <summary>
        /// Gets or sets the GhJSON schema version this patch targets.
        /// </summary>
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public string? Schema { get; set; }

        /// <summary>
        /// Gets or sets the discriminator. MUST be "ghpatch".
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; } = "ghpatch";

        /// <summary>
        /// Gets or sets the patch body containing the operations.
        /// </summary>
        [JsonProperty("patch")]
        public GhPatchBody Patch { get; set; } = new GhPatchBody();
    }
}
