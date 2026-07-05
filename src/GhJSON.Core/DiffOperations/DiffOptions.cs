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
    /// Options that control how two GhJSON documents are diffed.
    /// </summary>
    public sealed class DiffOptions
    {
        /// <summary>
        /// Gets the default diff options.
        /// </summary>
        public static DiffOptions Default { get; } = new DiffOptions();

        /// <summary>
        /// Gets or sets a value indicating whether runtime messages (errors, warnings, remarks)
        /// on components should be ignored when computing the diff.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool IgnoreRuntimeMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether metadata counters
        /// (<c>componentCount</c>, <c>connectionCount</c>, <c>groupCount</c>) should be ignored.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool IgnoreMetadataCounters { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether metadata timestamps
        /// (<c>created</c>, <c>modified</c>) should be ignored.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool IgnoreMetadataTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether component pivots should be ignored.
        /// Pivots represent canvas positions; defaults to <c>false</c> (pivots ARE diffed).
        /// </summary>
        public bool IgnorePivots { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the resulting patch should include
        /// a <c>patch.base</c> reference with the source document's normalised-form checksum.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool IncludeBaseChecksum { get; set; } = true;
    }
}
