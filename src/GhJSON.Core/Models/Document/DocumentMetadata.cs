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

using System.Collections.Generic;
using GhJSON.Core.Serialization;
using Newtonsoft.Json;

namespace GhJSON.Core.Models.Document
{
    /// <summary>
    /// Represents metadata for a Grasshopper document.
    /// </summary>
    public class DocumentMetadata
    {
        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the document.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the document version.
        /// </summary>
        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp in ISO 8601 format.
        /// </summary>
        [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Created { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp in ISO 8601 format.
        /// </summary>
        [JsonProperty("modified", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Modified { get; set; }

        /// <summary>
        /// Gets or sets the author of the document.
        /// </summary>
        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the Rhino version.
        /// </summary>
        [JsonProperty("rhinoVersion", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? RhinoVersion { get; set; }

        /// <summary>
        /// Gets or sets the Grasshopper version.
        /// </summary>
        [JsonProperty("grasshopperVersion", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? GrasshopperVersion { get; set; }

        /// <summary>
        /// Gets or sets the list of tags for categorizing and searching definitions.
        /// </summary>
        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the list of required plugin dependencies.
        /// </summary>
        [JsonProperty("dependencies", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the number of components in the document.
        /// </summary>
        [JsonProperty("componentCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? ComponentCount { get; set; }

        /// <summary>
        /// Gets or sets the number of connections in the document.
        /// </summary>
        [JsonProperty("connectionCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConnectionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of groups in the document.
        /// </summary>
        [JsonProperty("groupCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? GroupCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the plugin that generated this file.
        /// </summary>
        [JsonProperty("plugin", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? Plugin { get; set; }

        /// <summary>
        /// Gets or sets the plugin version that generated this file.
        /// </summary>
        [JsonProperty("pluginVersion", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(EmptyStringIgnoreConverter))]
        public string? PluginVersion { get; set; }
    }
}
