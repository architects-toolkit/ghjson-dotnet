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

using System;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.PatchModels
{
    /// <summary>
    /// Identifies a single component on the base document.
    /// </summary>
    /// <remarks>
    /// Identity precedence is <c>instanceGuid</c> &gt; <c>id</c> &gt; structural
    /// fingerprint (<c>componentGuid</c> + <c>name</c> + optional <c>pivot</c>).
    /// </remarks>
    public sealed class GhPatchComponentMatch
    {
        /// <summary>
        /// Gets or sets the instance GUID of the component to match.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the integer ID of the component to match.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the component GUID (type) of the component to match.
        /// </summary>
        [JsonProperty("componentGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? ComponentGuid { get; set; }

        /// <summary>
        /// Gets or sets the component name to match.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets an optional pivot fingerprint hint used as a tie-breaker.
        /// </summary>
        [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(PivotConverter))]
        public GhJsonPivot? Pivot { get; set; }
    }
}
