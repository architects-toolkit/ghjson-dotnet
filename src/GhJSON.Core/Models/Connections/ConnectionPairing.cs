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
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Connections
{
    /// <summary>
    /// Represents a connection between two components in a Grasshopper document.
    /// </summary>
    public class ConnectionPairing
    {
        /// <summary>
        /// Gets or sets the source endpoint of the connection.
        /// </summary>
        [JsonProperty("from")]
        [JsonRequired]
        public Connection From { get; set; } = null!;

        /// <summary>
        /// Gets or sets the target endpoint of the connection.
        /// </summary>
        [JsonProperty("to")]
        [JsonRequired]
        public Connection To { get; set; } = null!;

        /// <summary>
        /// Checks if both endpoints of the connection are valid.
        /// </summary>
        /// <returns>True if both the source and target endpoints are valid.</returns>
        public bool IsValid()
        {
            return this.To.IsValid() && this.From.IsValid();
        }

        /// <summary>
        /// Resolves the connection endpoints from integer IDs to GUIDs using the provided mapping.
        /// </summary>
        /// <param name="idToGuidMapping">Mapping from integer ID to GUID.</param>
        /// <param name="fromGuid">Output: The resolved source component GUID.</param>
        /// <param name="toGuid">Output: The resolved target component GUID.</param>
        /// <returns>True if both endpoints were successfully resolved.</returns>
        public bool TryResolveGuids(Dictionary<int, Guid> idToGuidMapping, out Guid fromGuid, out Guid toGuid)
        {
            fromGuid = Guid.Empty;
            toGuid = Guid.Empty;

            return idToGuidMapping.TryGetValue(this.From.Id, out fromGuid) &&
                   idToGuidMapping.TryGetValue(this.To.Id, out toGuid);
        }
    }
}
