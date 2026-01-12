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

using System.Linq;
using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations.FixOperations
{
    /// <summary>
    /// Assigns sequential IDs to components that don't have valid IDs.
    /// </summary>
    public class IdAssigner : IDocumentOperation
    {
        /// <inheritdoc/>
        public string Name => "IdAssigner";

        /// <inheritdoc/>
        public string Description => "Assigns sequential IDs to components missing valid IDs";

        /// <inheritdoc/>
        public OperationResult Apply(GhJsonDocument document)
        {
            if (document?.Components == null || document.Components.Count == 0)
                return OperationResult.NoChange();

            // Find max existing ID
            int maxId = document.Components
                .Where(c => c.Id > 0)
                .Select(c => c.Id)
                .DefaultIfEmpty(0)
                .Max();

            int assignedCount = 0;
            int nextId = maxId + 1;

            foreach (var component in document.Components)
            {
                if (component.Id <= 0)
                {
                    component.Id = nextId++;
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                return OperationResult.Changed(assignedCount, $"Assigned IDs to {assignedCount} component(s)");
            }

            return OperationResult.NoChange();
        }

        /// <summary>
        /// Reassigns all IDs sequentially starting from 1.
        /// </summary>
        /// <param name="document">The document to modify.</param>
        /// <returns>Operation result.</returns>
        public OperationResult ReassignAll(GhJsonDocument document)
        {
            if (document?.Components == null || document.Components.Count == 0)
                return OperationResult.NoChange();

            int id = 1;
            foreach (var component in document.Components)
            {
                component.Id = id++;
            }

            return OperationResult.Changed(document.Components.Count, $"Reassigned IDs to all {document.Components.Count} component(s)");
        }
    }
}
