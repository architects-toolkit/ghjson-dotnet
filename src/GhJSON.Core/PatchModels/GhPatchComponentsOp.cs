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
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json;

namespace GhJSON.Core.PatchModels
{
    /// <summary>
    /// Operations on the components array.
    /// </summary>
    public sealed class GhPatchComponentsOp
    {
        /// <summary>
        /// Gets or sets components to add. Each entry is a full component object.
        /// </summary>
        [JsonProperty("add", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhJsonComponent>? Add { get; set; }

        /// <summary>
        /// Gets or sets components to remove. Each entry is a component match.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhPatchComponentMatch>? Remove { get; set; }

        /// <summary>
        /// Gets or sets components to modify in place.
        /// </summary>
        [JsonProperty("modify", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhPatchComponentModify>? Modify { get; set; }
    }
}
