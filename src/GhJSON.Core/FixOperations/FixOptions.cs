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

namespace GhJSON.Core.FixOperations
{
    /// <summary>
    /// Options for fix operations on GhJSON documents.
    /// </summary>
    public sealed class FixOptions
    {
        /// <summary>
        /// Gets the default fix options.
        /// </summary>
        public static FixOptions Default { get; } = new FixOptions();

        /// <summary>
        /// Gets or sets a value indicating whether to assign missing IDs.
        /// </summary>
        public bool AssignMissingIds { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to reassign all IDs sequentially.
        /// </summary>
        public bool ReassignIds { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to generate missing instance GUIDs.
        /// </summary>
        public bool GenerateMissingInstanceGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to regenerate all instance GUIDs.
        /// </summary>
        public bool RegenerateInstanceGuids { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to fix metadata (counts, timestamps).
        /// </summary>
        public bool FixMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to remove invalid connections.
        /// </summary>
        public bool RemoveInvalidConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to remove invalid group members.
        /// </summary>
        public bool RemoveInvalidGroupMembers { get; set; } = true;
    }
}
