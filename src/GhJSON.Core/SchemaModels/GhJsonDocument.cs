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
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents a complete GhJSON document with components and their connections.
    /// This is the root object of a GhJSON file.
    /// </summary>
    public sealed class GhJsonDocument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GhJsonDocument"/> class.
        /// Intended for JSON deserialization.
        /// </summary>
        public GhJsonDocument()
            : this(
                schema: SchemaMigration.SchemaMigrator.CurrentVersion,
                metadata: null,
                components: Array.Empty<GhJsonComponent>(),
                connections: null,
                groups: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GhJsonDocument"/> class.
        /// </summary>
        /// <param name="schema">The GhJSON schema version.</param>
        /// <param name="metadata">The document metadata.</param>
        /// <param name="components">The document components.</param>
        /// <param name="connections">The document connections.</param>
        /// <param name="groups">The document groups.</param>
        [JsonConstructor]
        public GhJsonDocument(
            string? schema,
            GhJsonMetadata? metadata,
            IEnumerable<GhJsonComponent>? components,
            IEnumerable<GhJsonConnection>? connections,
            IEnumerable<GhJsonGroup>? groups)
        {
            this.Schema = schema;
            this.Metadata = metadata;
            this.Components = (components ?? Array.Empty<GhJsonComponent>()).ToList().AsReadOnly();

            var connectionsList = connections?.ToList();
            this.Connections = connectionsList?.Count > 0 ? connectionsList.AsReadOnly() : null;

            var groupsList = groups?.ToList();
            this.Groups = groupsList?.Count > 0 ? groupsList.AsReadOnly() : null;
        }

        /// <summary>
        /// Gets or sets the GhJSON schema version.
        /// </summary>
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public string? Schema { get; private set; }

        /// <summary>
        /// Gets or sets the document metadata (optional).
        /// </summary>
        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public GhJsonMetadata? Metadata { get; private set; }

        /// <summary>
        /// Gets or sets list of all components in the document.
        /// This is the primary content of a GhJSON file.
        /// </summary>
        [JsonProperty("components")]
        public IReadOnlyList<GhJsonComponent> Components { get; private set; }

        /// <summary>
        /// Gets or sets list of all connections between components in the document.
        /// </summary>
        [JsonProperty("connections", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<GhJsonConnection>? Connections { get; private set; }

        /// <summary>
        /// Gets or sets list of all groups in the document (optional).
        /// </summary>
        [JsonProperty("groups", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<GhJsonGroup>? Groups { get; private set; }

        /// <summary>
        /// Gets all components with validation issues (errors or warnings).
        /// </summary>
        /// <returns>A list of components that have either errors or warnings.</returns>
        public List<GhJsonComponent> GetComponentsWithIssues()
        {
            return this.Components.Where(c => c.HasIssues).ToList();
        }

        /// <summary>
        /// Gets all connections for a specific component.
        /// </summary>
        /// <param name="componentId">The integer ID of the component to get connections for.</param>
        /// <returns>A list of all connections involving the specified component.</returns>
        public List<GhJsonConnection> GetComponentConnections(int componentId)
        {
            if (this.Connections == null)
            {
                return new List<GhJsonConnection>();
            }

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
        public List<GhJsonConnection> GetComponentInputs(int componentId)
        {
            if (this.Connections == null)
            {
                return new List<GhJsonConnection>();
            }

            return this.Connections.Where(c => c.To.Id == componentId).ToList();
        }

        /// <summary>
        /// Gets all output connections for a specific component.
        /// </summary>
        /// <param name="componentId">The integer ID of the component to get output connections for.</param>
        /// <returns>A list of connections where the specified component is the source.</returns>
        public List<GhJsonConnection> GetComponentOutputs(int componentId)
        {
            if (this.Connections == null)
            {
                return new List<GhJsonConnection>();
            }

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
                .Where(c => c.Id.HasValue && c.InstanceGuid.HasValue)
                .ToDictionary(c => c.Id!.Value, c => c.InstanceGuid!.Value);
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
                .Where(c => c.Id.HasValue && c.InstanceGuid.HasValue)
                .ToDictionary(c => c.InstanceGuid!.Value, c => c.Id!.Value);
        }
    }
}
