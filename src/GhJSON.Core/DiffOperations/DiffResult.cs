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

using GhJSON.Core.PatchModels;

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Result of comparing two GhJSON documents.
    /// </summary>
    public sealed class DiffResult
    {
        /// <summary>
        /// Gets or sets the generated patch document. Empty if the two documents are equivalent
        /// under the active <see cref="DiffOptions"/>.
        /// </summary>
        public GhPatchDocument Patch { get; set; } = new GhPatchDocument();

        /// <summary>
        /// Gets or sets the number of component-level operations included in the patch.
        /// </summary>
        public int ComponentOpCount { get; set; }

        /// <summary>
        /// Gets or sets the number of connection-level operations included in the patch.
        /// </summary>
        public int ConnectionOpCount { get; set; }

        /// <summary>
        /// Gets or sets the number of group-level operations included in the patch.
        /// </summary>
        public int GroupOpCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether any operations were produced.
        /// </summary>
        public bool HasChanges =>
            this.ComponentOpCount > 0 ||
            this.ConnectionOpCount > 0 ||
            this.GroupOpCount > 0 ||
            this.Patch.Patch.Metadata != null;
    }
}
