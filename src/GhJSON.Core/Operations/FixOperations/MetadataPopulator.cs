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
using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations.FixOperations
{
    /// <summary>
    /// Populates metadata fields in a GhJSON document.
    /// </summary>
    public class MetadataPopulator : IDocumentOperation
    {
        /// <inheritdoc/>
        public string Name => "MetadataPopulator";

        /// <inheritdoc/>
        public string Description => "Populates metadata fields (timestamps, version, etc.)";

        /// <inheritdoc/>
        public OperationResult Apply(GhJsonDocument document)
        {
            if (document == null)
                return OperationResult.NoChange();

            bool modified = false;

            // Ensure metadata object exists
            if (document.Metadata == null)
            {
                document.Metadata = new DocumentMetadata();
                modified = true;
            }

            // Set schema version if missing
            if (string.IsNullOrEmpty(document.SchemaVersion))
            {
                document.SchemaVersion = "1.0";
                modified = true;
            }

            // Update component count
            if (document.Components != null)
            {
                var newCount = document.Components.Count;
                if (document.Metadata.ComponentCount != newCount)
                {
                    document.Metadata.ComponentCount = newCount;
                    modified = true;
                }
            }

            // Set modified timestamp
            var now = DateTime.UtcNow.ToString("o");
            document.Metadata.Modified = now;
            modified = true;

            // Set created timestamp if not present
            if (string.IsNullOrEmpty(document.Metadata.Created))
            {
                document.Metadata.Created = now;
            }

            if (modified)
            {
                return OperationResult.Changed(1, "Populated metadata fields");
            }

            return OperationResult.NoChange();
        }
    }
}
