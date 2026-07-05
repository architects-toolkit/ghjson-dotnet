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

namespace GhJSON.Core.DiffOperations
{
    /// <summary>
    /// Options that control how a GhPatch document is applied to a base document.
    /// </summary>
    public sealed class ApplyPatchOptions
    {
        /// <summary>
        /// Gets the default apply options.
        /// </summary>
        public static ApplyPatchOptions Default { get; } = new ApplyPatchOptions();

        /// <summary>
        /// Gets or sets a value indicating whether to verify the base-document checksum if the
        /// patch carries one. When <c>true</c> and the checksum mismatches, application is refused.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool VerifyBase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to keep applying once a recoverable conflict
        /// has been recorded. Defaults to <c>true</c>.
        /// </summary>
        public bool ContinueOnConflict { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to renumber integer IDs of components added
        /// by the patch when they collide with existing IDs in the base. Defaults to <c>true</c>.
        /// </summary>
        public bool RenumberCollidingAddedIds { get; set; } = true;
    }
}
