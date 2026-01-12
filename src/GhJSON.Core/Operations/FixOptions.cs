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

namespace GhJSON.Core.Operations
{
    /// <summary>
    /// Options for fix operations on GhJSON documents.
    /// </summary>
    public class FixOptions
    {
        /// <summary>
        /// Gets or sets whether to assign sequential IDs to components.
        /// Default: true
        /// </summary>
        public bool AssignIds { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate instanceGuids for components that lack them.
        /// Default: true
        /// </summary>
        public bool GenerateInstanceGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to populate metadata (timestamps, etc.).
        /// Default: true
        /// </summary>
        public bool PopulateMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to update component and connection counts in metadata.
        /// Default: true
        /// </summary>
        public bool UpdateCounts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate and fix connections.
        /// Default: true
        /// </summary>
        public bool ValidateConnections { get; set; } = true;

        /// <summary>
        /// Gets the default fix options with all fixes enabled.
        /// </summary>
        public static FixOptions Default => new FixOptions();

        /// <summary>
        /// Gets minimal fix options (only essential fixes).
        /// </summary>
        public static FixOptions Minimal => new FixOptions
        {
            AssignIds = true,
            GenerateInstanceGuids = false,
            PopulateMetadata = false,
            UpdateCounts = false,
            ValidateConnections = false
        };
    }
}
