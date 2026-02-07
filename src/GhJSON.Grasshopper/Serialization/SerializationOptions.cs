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

namespace GhJSON.Grasshopper.Serialization
{
    /// <summary>
    /// Options for serializing Grasshopper objects to GhJSON.
    /// </summary>
    public sealed class SerializationOptions
    {
        /// <summary>
        /// Gets the default serialization options.
        /// </summary>
        public static SerializationOptions Default { get; } = new SerializationOptions();

        /// <summary>
        /// Gets or sets a value indicating whether to include internalized data.
        /// </summary>
        public bool IncludeInternalizedData { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include runtime messages.
        /// </summary>
        public bool IncludeRuntimeMessages { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include selected state.
        /// </summary>
        public bool IncludeSelectedState { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include connections.
        /// </summary>
        public bool IncludeConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include groups.
        /// </summary>
        public bool IncludeGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to assign sequential IDs.
        /// </summary>
        public bool AssignSequentialIds { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include document metadata
        /// (Rhino/Grasshopper versions, timestamps, plugin dependencies, counts).
        /// </summary>
        public bool IncludeMetadata { get; set; } = false;

        #region Metadata overrides

        /// <summary>
        /// Gets or sets an explicit title for the metadata.
        /// When null, defaults to the Grasshopper document file name (without extension).
        /// </summary>
        public string? MetadataTitle { get; set; }

        /// <summary>
        /// Gets or sets an explicit description for the metadata.
        /// </summary>
        public string? MetadataDescription { get; set; }

        /// <summary>
        /// Gets or sets an explicit definition version string for the metadata.
        /// </summary>
        public string? MetadataVersion { get; set; }

        /// <summary>
        /// Gets or sets an explicit author for the metadata.
        /// </summary>
        public string? MetadataAuthor { get; set; }

        /// <summary>
        /// Gets or sets explicit tags for the metadata.
        /// </summary>
        public List<string>? MetadataTags { get; set; }

        /// <summary>
        /// Gets or sets an explicit generator name for the metadata.
        /// When null, defaults to "ghjson-dotnet".
        /// </summary>
        public string? MetadataGeneratorName { get; set; }

        /// <summary>
        /// Gets or sets an explicit generator version for the metadata.
        /// When null, defaults to the ghjson-dotnet assembly version.
        /// </summary>
        public string? MetadataGeneratorVersion { get; set; }

        #endregion
    }
}
