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

namespace GhJSON.Core.MergeOperations
{
    /// <summary>
    /// Options for merging GhJSON documents.
    /// </summary>
    public sealed class MergeOptions
    {
        /// <summary>
        /// Gets the default merge options.
        /// </summary>
        public static MergeOptions Default { get; } = new MergeOptions();

        /// <summary>
        /// Gets or sets a value indicating whether to regenerate IDs for incoming components.
        /// </summary>
        public bool RegenerateIds { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to regenerate instance GUIDs for incoming components.
        /// </summary>
        public bool RegenerateInstanceGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to merge metadata from the incoming document.
        /// </summary>
        public bool MergeMetadata { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to preserve groups from the incoming document.
        /// </summary>
        public bool PreserveGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets the X offset to apply to incoming component positions.
        /// </summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>
        /// Gets or sets the Y offset to apply to incoming component positions.
        /// </summary>
        public double OffsetY { get; set; } = 0;
    }
}
