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

namespace GhJSON.Core.Operations
{
    /// <summary>
    /// Contract for document operations that modify a GhJSON document.
    /// </summary>
    public interface IDocumentOperation
    {
        /// <summary>
        /// Gets the name of this operation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a description of what this operation does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Applies this operation to the document.
        /// </summary>
        /// <param name="document">The document to modify.</param>
        /// <returns>Result indicating what was changed.</returns>
        OperationResult Apply(GhJsonDocument document);
    }

    /// <summary>
    /// Result of a document operation.
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// Gets or sets whether the operation modified the document.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Gets or sets a description of what was changed.
        /// </summary>
        public string? ChangeDescription { get; set; }

        /// <summary>
        /// Gets or sets the count of items affected.
        /// </summary>
        public int ItemsAffected { get; set; }

        /// <summary>
        /// Creates a result indicating no changes were made.
        /// </summary>
        public static OperationResult NoChange() => new OperationResult { WasModified = false };

        /// <summary>
        /// Creates a result indicating changes were made.
        /// </summary>
        public static OperationResult Changed(int itemsAffected, string description) =>
            new OperationResult
            {
                WasModified = true,
                ItemsAffected = itemsAffected,
                ChangeDescription = description
            };
    }
}
