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
using System.Collections.Generic;
using GhJSON.Core.SchemaModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhJSON.Core.PatchModels
{
    /// <summary>
    /// Operations on the groups array.
    /// </summary>
    public sealed class GhPatchGroupsOp
    {
        /// <summary>
        /// Gets or sets groups to add.
        /// </summary>
        [JsonProperty("add", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhJsonGroup>? Add { get; set; }

        /// <summary>
        /// Gets or sets groups to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhPatchGroupMatch>? Remove { get; set; }

        /// <summary>
        /// Gets or sets groups to modify in place.
        /// </summary>
        [JsonProperty("modify", NullValueHandling = NullValueHandling.Ignore)]
        public List<GhPatchGroupModify>? Modify { get; set; }
    }

    /// <summary>
    /// Identifies a single group. Identity precedence is <c>instanceGuid</c> &gt; <c>id</c>.
    /// </summary>
    public sealed class GhPatchGroupMatch
    {
        /// <summary>
        /// Gets or sets the instance GUID of the group to match.
        /// </summary>
        [JsonProperty("instanceGuid", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InstanceGuid { get; set; }

        /// <summary>
        /// Gets or sets the integer ID of the group to match.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; set; }
    }

    /// <summary>
    /// Modify a single group matched by identity.
    /// </summary>
    public sealed class GhPatchGroupModify
    {
        /// <summary>
        /// Gets or sets the group identity to modify.
        /// </summary>
        [JsonProperty("match")]
        public GhPatchGroupMatch Match { get; set; } = new GhPatchGroupMatch();

        /// <summary>
        /// Gets or sets top-level group fields to set.
        /// </summary>
        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Set { get; set; }

        /// <summary>
        /// Gets or sets top-level group field names to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Remove { get; set; }

        /// <summary>
        /// Gets or sets operations on the group's <c>members</c> list.
        /// </summary>
        [JsonProperty("members", NullValueHandling = NullValueHandling.Ignore)]
        public GhPatchGroupMembersOp? Members { get; set; }
    }

    /// <summary>
    /// Operations on a group's <c>members</c> integer-id list.
    /// </summary>
    public sealed class GhPatchGroupMembersOp
    {
        /// <summary>
        /// Gets or sets the member component IDs to add.
        /// </summary>
        [JsonProperty("add", NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? Add { get; set; }

        /// <summary>
        /// Gets or sets the member component IDs to remove.
        /// </summary>
        [JsonProperty("remove", NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? Remove { get; set; }
    }
}
