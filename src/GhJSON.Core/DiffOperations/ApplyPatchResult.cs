/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2026 Marc Roca Musach
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
using System.Linq;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Result of applying a GhPatch document to a base document.
    /// </summary>
    public sealed class ApplyPatchResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the patch applied with no fatal errors.
        /// A patch may still produce non-fatal conflicts; check <see cref="HasConflicts"/>.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the resulting document (may be a partial application if conflicts occurred).
        /// </summary>
        public GhJsonDocument Document { get; set; } = new GhJsonDocument();

        /// <summary>
        /// Gets the list of conflicts recorded during apply.
        /// </summary>
        public List<PatchConflict> Conflicts { get; } = new List<PatchConflict>();

        /// <summary>Gets the number of components added.</summary>
        public int ComponentsAdded { get; set; }

        /// <summary>Gets the number of components removed.</summary>
        public int ComponentsRemoved { get; set; }

        /// <summary>Gets the number of components modified.</summary>
        public int ComponentsModified { get; set; }

        /// <summary>Gets the number of connections added.</summary>
        public int ConnectionsAdded { get; set; }

        /// <summary>Gets the number of connections removed.</summary>
        public int ConnectionsRemoved { get; set; }

        /// <summary>Gets the number of groups added.</summary>
        public int GroupsAdded { get; set; }

        /// <summary>Gets the number of groups removed.</summary>
        public int GroupsRemoved { get; set; }

        /// <summary>Gets the number of groups modified.</summary>
        public int GroupsModified { get; set; }

        /// <summary>Gets a value indicating whether any conflict was recorded.</summary>
        public bool HasConflicts => this.Conflicts.Any();
    }
}
