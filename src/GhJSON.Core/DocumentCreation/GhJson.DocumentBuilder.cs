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
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Validation;

namespace GhJSON.Core
{
    public static partial class GhJson
    {
        /// <summary>
        /// Fluent builder for <see cref="GhJsonDocument"/>.
        /// Produces immutable document instances without side effects.
        /// </summary>
        public sealed class DocumentBuilder
        {
            private readonly string? schema;
            private readonly GhJsonMetadata? metadata;
            private readonly IReadOnlyList<GhJsonComponent> components;
            private readonly IReadOnlyList<GhJsonConnection>? connections;
            private readonly IReadOnlyList<GhJsonGroup>? groups;

            private DocumentBuilder(
                string? schema,
                GhJsonMetadata? metadata,
                IReadOnlyList<GhJsonComponent> components,
                IReadOnlyList<GhJsonConnection>? connections,
                IReadOnlyList<GhJsonGroup>? groups)
            {
                this.schema = schema;
                this.metadata = metadata;
                this.components = components ?? new List<GhJsonComponent>();
                this.connections = connections;
                this.groups = groups;
            }

            /// <summary>
            /// Creates a new builder initialized for a new document.
            /// </summary>
            /// <returns>A new builder instance.</returns>
            public static DocumentBuilder Create()
            {
                return new DocumentBuilder(schema: CurrentVersion, metadata: null, components: new List<GhJsonComponent>(), connections: null, groups: null);
            }

            /// <summary>
            /// Creates a new builder initialized from an existing immutable document.
            /// </summary>
            /// <param name="document">Source document to copy values from. If null, a new empty builder is returned.</param>
            /// <returns>A new builder instance.</returns>
            public static DocumentBuilder FromImmutable(GhJsonDocument? document)
            {
                if (document == null)
                {
                    return Create();
                }

                return new DocumentBuilder(
                    schema: string.IsNullOrWhiteSpace(document.Schema) ? CurrentVersion : document.Schema,
                    metadata: document.Metadata,
                    components: document.Components.ToList(),
                    connections: document.Connections?.ToList(),
                    groups: document.Groups?.ToList());
            }

            /// <summary>
            /// Returns a copy of this builder with the schema version set.
            /// </summary>
            /// <param name="schema">Schema version to set. Null/whitespace keeps the current schema.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder WithSchema(string? schema)
            {
                return string.IsNullOrWhiteSpace(schema)
                    ? this
                    : new DocumentBuilder(schema: schema, metadata: this.metadata, components: this.components, connections: this.connections, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the metadata set.
            /// </summary>
            /// <param name="metadata">Metadata to set (optional).</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder WithMetadata(GhJsonMetadata? metadata)
            {
                return new DocumentBuilder(schema: this.schema, metadata: metadata, components: this.components, connections: this.connections, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the component appended.
            /// </summary>
            /// <param name="component">Component to add. Null is ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddComponent(GhJsonComponent? component)
            {
                if (component == null)
                {
                    return this;
                }

                var next = this.components.ToList();
                next.Add(component);
                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: next, connections: this.connections, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the components appended.
            /// </summary>
            /// <param name="items">Components to add. Null items are ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddComponents(IEnumerable<GhJsonComponent>? items)
            {
                if (items == null)
                {
                    return this;
                }

                var next = this.components.ToList();
                foreach (var c in items)
                {
                    if (c != null)
                    {
                        next.Add(c);
                    }
                }

                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: next, connections: this.connections, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the connection appended.
            /// </summary>
            /// <param name="connection">Connection to add. Null is ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddConnection(GhJsonConnection? connection)
            {
                if (connection == null)
                {
                    return this;
                }

                var next = (this.connections ?? new List<GhJsonConnection>()).ToList();
                next.Add(connection);
                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: this.components, connections: next, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the connections appended.
            /// </summary>
            /// <param name="items">Connections to add. Null items are ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddConnections(IEnumerable<GhJsonConnection>? items)
            {
                if (items == null)
                {
                    return this;
                }

                var next = (this.connections ?? new List<GhJsonConnection>()).ToList();
                foreach (var c in items)
                {
                    if (c != null)
                    {
                        next.Add(c);
                    }
                }

                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: this.components, connections: next, groups: this.groups);
            }

            /// <summary>
            /// Returns a copy of this builder with the group appended.
            /// </summary>
            /// <param name="group">Group to add. Null is ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddGroup(GhJsonGroup? group)
            {
                if (group == null)
                {
                    return this;
                }

                var next = (this.groups ?? new List<GhJsonGroup>()).ToList();
                next.Add(group);
                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: this.components, connections: this.connections, groups: next);
            }

            /// <summary>
            /// Returns a copy of this builder with the groups appended.
            /// </summary>
            /// <param name="items">Groups to add. Null items are ignored.</param>
            /// <returns>A new builder instance.</returns>
            public DocumentBuilder AddGroups(IEnumerable<GhJsonGroup>? items)
            {
                if (items == null)
                {
                    return this;
                }

                var next = (this.groups ?? new List<GhJsonGroup>()).ToList();
                foreach (var g in items)
                {
                    if (g != null)
                    {
                        next.Add(g);
                    }
                }

                return new DocumentBuilder(schema: this.schema, metadata: this.metadata, components: this.components, connections: this.connections, groups: next);
            }

            /// <summary>
            /// Builds an immutable <see cref="GhJsonDocument"/> and validates basic reference integrity.
            /// </summary>
            /// <returns>The built document.</returns>
            /// <exception cref="InvalidOperationException">Thrown when the builder produces an invalid document.</exception>
            public GhJsonDocument Build()
            {
                var doc = new GhJsonDocument(
                    schema: string.IsNullOrWhiteSpace(this.schema) ? CurrentVersion : this.schema,
                    metadata: this.metadata,
                    components: this.components,
                    connections: this.connections?.Any() == true ? this.connections : null,
                    groups: this.groups?.Any() == true ? this.groups : null);

                var validation = GhJsonValidator.Validate(doc, ValidationLevel.Standard);
                if (validation.HasErrors)
                {
                    var errors = string.Join("; ", validation.Errors.Select(e => e.ToString()));
                    throw new InvalidOperationException($"Built document is invalid: {errors}");
                }

                return doc;
            }
        }
    }
}
