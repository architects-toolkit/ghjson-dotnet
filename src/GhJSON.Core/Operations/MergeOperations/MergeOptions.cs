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

namespace GhJSON.Core.Operations.MergeOperations
{
    /// <summary>
    /// Options for merge operations.
    /// </summary>
    public class MergeOptions
    {
        /// <summary>
        /// Gets or sets the conflict resolution strategy.
        /// </summary>
        public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.TargetWins;

        /// <summary>
        /// Gets or sets whether to adjust positions to avoid overlap.
        /// </summary>
        public bool AdjustPositions { get; set; } = true;

        /// <summary>
        /// Gets or sets the position offset when adjusting positions.
        /// </summary>
        public float PositionOffset { get; set; } = 300f;

        /// <summary>
        /// Gets or sets whether to reassign IDs to avoid conflicts.
        /// </summary>
        public bool ReassignIds { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to merge groups.
        /// </summary>
        public bool MergeGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to merge metadata.
        /// </summary>
        public bool MergeMetadata { get; set; } = false;

        /// <summary>
        /// Gets the default merge options.
        /// </summary>
        public static MergeOptions Default => new MergeOptions();
    }

    /// <summary>
    /// Conflict resolution strategies for merge operations.
    /// </summary>
    public enum ConflictResolution
    {
        /// <summary>Target document components take precedence (skip duplicates from source).</summary>
        TargetWins,

        /// <summary>Source document components replace target on conflict.</summary>
        SourceWins,

        /// <summary>Keep both components (generate new GUIDs for source).</summary>
        KeepBoth,

        /// <summary>Fail on conflict.</summary>
        Fail
    }
}
