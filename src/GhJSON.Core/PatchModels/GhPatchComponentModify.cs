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
    /// Modify a single component matched by identity.
    /// </summary>
    public sealed class GhPatchComponentModify
    {
        /// <summary>
        /// Gets or sets the component identity to modify.
        /// </summary>
        [JsonProperty("match")]
        public GhPatchComponentMatch Match { get; set; } = new GhPatchComponentMatch();

        /// <summary>
        /// Gets or sets top-level component fields to set.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Set { get; set; }

        /// <summary>
        /// Gets or sets top-level component field names to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }

        /// <summary>
        /// Gets or sets operations on the component's <c>componentState</c>.
        /// </summary>
        [JsonProperty("componentState", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchComponentStateOp? ComponentState { get; set; }

        /// <summary>
        /// Gets or sets operations on the component's <c>inputSettings</c> list.
        /// </summary>
        [JsonProperty("inputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchParameterSettingsOp? InputSettings { get; set; }

        /// <summary>
        /// Gets or sets operations on the component's <c>outputSettings</c> list.
        /// </summary>
        [JsonProperty("outputSettings", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchParameterSettingsOp? OutputSettings { get; set; }
    }

    /// <summary>
    /// Operations on a component's <c>componentState</c>.
    /// </summary>
    public sealed class GhPatchComponentStateOp
    {
        /// <summary>
        /// Gets or sets top-level componentState fields to set.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Set { get; set; }

        /// <summary>
        /// Gets or sets top-level componentState field names to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }

        /// <summary>
        /// Gets or sets extension operations.
        /// </summary>
        [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchExtensionsOp? Extensions { get; set; }
    }

    /// <summary>
    /// Operations on an extensions container.
    /// </summary>
    public sealed class GhPatchExtensionsOp
    {
        /// <summary>
        /// Gets or sets the map of extension key to new extension value object.
        /// Each extension value is treated as opaque: the entire object is replaced on set.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JObject>? Set { get; set; }

        /// <summary>
        /// Gets or sets extension keys to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }
    }

    /// <summary>
    /// Operations on an inputSettings or outputSettings list, keyed by parameter name.
    /// </summary>
    public sealed class GhPatchParameterSettingsOp
    {
        /// <summary>
        /// Gets or sets a map of <c>parameterName</c> to a per-parameter operation.
        /// </summary>
        [JsonProperty("byParameterName", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, GhPatchParameterSettingsEntryOp>? ByParameterName { get; set; }
    }

    /// <summary>
    /// Per-parameter set/remove on a parameterSettings entry.
    /// </summary>
    public sealed class GhPatchParameterSettingsEntryOp
    {
        /// <summary>
        /// Gets or sets parameter settings fields to set.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Set { get; set; }

        /// <summary>
        /// Gets or sets parameter settings field names to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }
    }
}
