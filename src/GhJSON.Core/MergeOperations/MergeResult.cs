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

using System.Collections.Generic;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.MergeOperations
{
    /// <summary>
    /// Represents the result of merging two GhJSON documents.
    /// </summary>
    public sealed class MergeResult
    {
        /// <summary>
        /// Gets or sets the merged document.
        /// </summary>
        public GhJsonDocument Document { get; set; } = new GhJsonDocument();

        /// <summary>
        /// Gets or sets a value indicating whether the merge was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of components added from the incoming document.
        /// </summary>
        public int ComponentsAdded { get; set; }

        /// <summary>
        /// Gets or sets the number of connections added from the incoming document.
        /// </summary>
        public int ConnectionsAdded { get; set; }

        /// <summary>
        /// Gets or sets the number of groups added from the incoming document.
        /// </summary>
        public int GroupsAdded { get; set; }

        /// <summary>
        /// Gets or sets the list of conflicts that occurred during merge.
        /// </summary>
        public List<string> Conflicts { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the mapping of old IDs to new IDs for the incoming document.
        /// </summary>
        public Dictionary<int, int> IdMapping { get; set; } = new Dictionary<int, int>();
    }
}
