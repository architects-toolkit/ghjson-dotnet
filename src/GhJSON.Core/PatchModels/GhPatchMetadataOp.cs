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

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.PatchModels
{
    /// <summary>
    /// Set or remove metadata fields. Metadata is a single object per document,
    /// so there is no add/remove of items.
    /// </summary>
    public sealed class GhPatchMetadataOp
    {
        /// <summary>
        /// Gets or sets the metadata fields to set or replace.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Set { get; set; }

        /// <summary>
        /// Gets or sets the metadata field names to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }
    }
}
