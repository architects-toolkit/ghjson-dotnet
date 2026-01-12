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
    /// Generates instance GUIDs for components that don't have them.
    /// </summary>
    public class GuidGenerator : IDocumentOperation
    {
        /// <inheritdoc/>
        public string Name => "GuidGenerator";

        /// <inheritdoc/>
        public string Description => "Generates instance GUIDs for components missing them";

        /// <inheritdoc/>
        public OperationResult Apply(GhJsonDocument document)
        {
            if (document?.Components == null || document.Components.Count == 0)
                return OperationResult.NoChange();

            int generatedCount = 0;

            foreach (var component in document.Components)
            {
                if (!component.InstanceGuid.HasValue || component.InstanceGuid.Value == Guid.Empty)
                {
                    component.InstanceGuid = Guid.NewGuid();
                    generatedCount++;
                }
            }

            if (generatedCount > 0)
            {
                return OperationResult.Changed(generatedCount, $"Generated GUIDs for {generatedCount} component(s)");
            }

            return OperationResult.NoChange();
        }

        /// <summary>
        /// Regenerates all instance GUIDs, creating new unique values.
        /// </summary>
        /// <param name="document">The document to modify.</param>
        /// <returns>Operation result.</returns>
        public OperationResult RegenerateAll(GhJsonDocument document)
        {
            if (document?.Components == null || document.Components.Count == 0)
                return OperationResult.NoChange();

            foreach (var component in document.Components)
            {
                component.InstanceGuid = Guid.NewGuid();
            }

            return OperationResult.Changed(document.Components.Count, $"Regenerated GUIDs for all {document.Components.Count} component(s)");
        }
    }
}
