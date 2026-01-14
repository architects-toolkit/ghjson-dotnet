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

namespace GhJSON.Core.SchemaModels
{
    /// <summary>
    /// Represents metadata about a GhJSON document.
    /// Maps to the documentMetadata definition in the schema.
    /// </summary>
    public sealed class GhJsonMetadata
    {
        /// <summary>
        /// Gets or sets the title of the definition.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets a description of what this definition does.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the definition itself (not the schema).
        /// On every save, the version should be incremented.
        /// </summary>
        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the author of this definition.
        /// </summary>
        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp in ISO 8601 format.
        /// </summary>
        [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Created { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp in ISO 8601 format.
        /// </summary>
        [JsonProperty("modified", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Modified { get; set; }

        /// <summary>
        /// Gets or sets the Rhino version this definition was created with.
        /// </summary>
        [JsonProperty("rhinoVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string? RhinoVersion { get; set; }

        /// <summary>
        /// Gets or sets the Grasshopper version this definition was created with.
        /// </summary>
        [JsonProperty("grasshopperVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string? GrasshopperVersion { get; set; }

        /// <summary>
        /// Gets or sets list of tags for categorizing and searching definitions.
        /// </summary>
        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets list of required plugin dependencies.
        /// </summary>
        [JsonProperty("dependencies", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Dependencies { get; set; }

        /// <summary>
        /// Gets or sets total number of components in the document.
        /// </summary>
        [JsonProperty("componentCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? ComponentCount { get; set; }

        /// <summary>
        /// Gets or sets total number of connections in the document.
        /// </summary>
        [JsonProperty("connectionCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConnectionCount { get; set; }

        /// <summary>
        /// Gets or sets total number of groups in the document.
        /// </summary>
        [JsonProperty("groupCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? GroupCount { get; set; }

        /// <summary>
        /// Gets or sets name of the tool that generated this GhJSON file.
        /// </summary>
        [JsonProperty("generatorName", NullValueHandling = NullValueHandling.Ignore)]
        public string? GeneratorName { get; set; }

        /// <summary>
        /// Gets or sets version of the tool that generated this file.
        /// </summary>
        [JsonProperty("generatorVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string? GeneratorVersion { get; set; }

        /// <summary>
        /// Gets or sets extension data produced by object handlers.
        /// Keys are extension identifiers, values are extension-specific objects.
        /// </summary>
        [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? Extensions { get; set; }
    }
}
