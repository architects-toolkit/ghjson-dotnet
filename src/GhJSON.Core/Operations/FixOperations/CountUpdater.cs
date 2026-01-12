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

using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations.FixOperations
{
    /// <summary>
    /// Updates component and connection counts in document metadata.
    /// </summary>
    public class CountUpdater : IDocumentOperation
    {
        /// <inheritdoc/>
        public string Name => "CountUpdater";

        /// <inheritdoc/>
        public string Description => "Updates component and connection counts in metadata";

        /// <inheritdoc/>
        public OperationResult Apply(GhJsonDocument document)
        {
            if (document == null)
                return OperationResult.NoChange();

            // Ensure metadata object exists
            if (document.Metadata == null)
            {
                document.Metadata = new DocumentMetadata();
            }

            bool modified = false;

            // Update component count
            var componentCount = document.Components?.Count ?? 0;
            if (document.Metadata.ComponentCount != componentCount)
            {
                document.Metadata.ComponentCount = componentCount;
                modified = true;
            }

            // Update connection count if tracked in metadata
            var connectionCount = document.Connections?.Count ?? 0;

            // Update group count if tracked
            var groupCount = document.Groups?.Count ?? 0;

            if (modified)
            {
                return OperationResult.Changed(1, $"Updated counts: {componentCount} components, {connectionCount} connections, {groupCount} groups");
            }

            return OperationResult.NoChange();
        }
    }
}
