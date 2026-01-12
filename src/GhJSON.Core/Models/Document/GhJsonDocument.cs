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
using System.Linq;
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Connections;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Document
{
    /// <summary>
    /// Represents a complete GhJSON document with components and their connections.
    /// </summary>
    public class GhJsonDocument
    {
        /// <summary>
        /// Gets or sets the GhJSON schema version.
        /// </summary>
        [JsonProperty("schemaVersion", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the document metadata (optional).
        /// </summary>
        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public DocumentMetadata? Metadata { get; set; }

        /// <summary>
        /// Gets or sets list of all components in the document.
        /// </summary>
        [JsonProperty("components")]
        public List<ComponentProperties> Components { get; set; } = new List<ComponentProperties>();

        /// <summary>
        /// Gets or sets list of all connections between components in the document.
        /// </summary>
        [JsonProperty("connections")]
        public List<ConnectionPairing> Connections { get; set; } = new List<ConnectionPairing>();

        /// <summary>
        /// Gets or sets list of all groups in the document (optional).
        /// </summary>
        [JsonProperty("groups", NullValueHandling = NullValueHandling.Ignore)]
        public List<GroupInfo>? Groups { get; set; }

        /// <summary>
        /// Gets all components with validation issues (errors or warnings).
        /// </summary>
        /// <returns>A list of components that have either errors or warnings.</returns>
        public List<ComponentProperties> GetComponentsWithIssues()
        {
            return this.Components.Where(c => c.HasIssues).ToList();
        }

        /// <summary>
        /// Gets all connections for a specific component.
        /// </summary>
        /// <param name="componentId">The integer ID of the component to get connections for.</param>
        /// <returns>A list of all connections involving the specified component.</returns>
        public List<ConnectionPairing> GetComponentConnections(int componentId)
        {
            return this.Connections.Where(c =>
                c.From.Id == componentId ||
                c.To.Id == componentId)
            .ToList();
        }

        /// <summary>
        /// Gets all input connections for a specific component.
        /// </summary>
        /// <param name="componentId">The integer ID of the component to get input connections for.</param>
        /// <returns>A list of connections where the specified component is the target.</returns>
        public List<ConnectionPairing> GetComponentInputs(int componentId)
        {
            return this.Connections.Where(c => c.To.Id == componentId).ToList();
        }

        /// <summary>
        /// Gets all output connections for a specific component.
        /// </summary>
        /// <param name="componentId">The integer ID of the component to get output connections for.</param>
        /// <returns>A list of connections where the specified component is the source.</returns>
        public List<ConnectionPairing> GetComponentOutputs(int componentId)
        {
            return this.Connections.Where(c => c.From.Id == componentId).ToList();
        }

        /// <summary>
        /// Creates a mapping from component integer IDs to their instance GUIDs.
        /// This is useful for resolving connections that use integer IDs.
        /// Only includes components that have an instance GUID set.
        /// </summary>
        /// <returns>Dictionary mapping integer ID to GUID.</returns>
        public Dictionary<int, Guid> GetIdToGuidMapping()
        {
            return this.Components
                .Where(c => c.InstanceGuid.HasValue)
                .ToDictionary(c => c.Id, c => c.InstanceGuid!.Value);
        }

        /// <summary>
        /// Creates a mapping from component instance GUIDs to their integer IDs.
        /// This is useful for creating connections from component GUIDs.
        /// Only includes components that have an instance GUID set.
        /// </summary>
        /// <returns>Dictionary mapping GUID to integer ID.</returns>
        public Dictionary<Guid, int> GetGuidToIdMapping()
        {
            return this.Components
                .Where(c => c.InstanceGuid.HasValue)
                .ToDictionary(c => c.InstanceGuid!.Value, c => c.Id);
        }

        /// <summary>
        /// Serializes this document to a JSON string.
        /// </summary>
        /// <param name="formatting">The JSON formatting to use.</param>
        /// <returns>The JSON string representation of this document.</returns>
        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }

        /// <summary>
        /// Deserializes a JSON string to a GhJsonDocument.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized GhJsonDocument.</returns>
        public static GhJsonDocument? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<GhJsonDocument>(json);
        }
    }
}
