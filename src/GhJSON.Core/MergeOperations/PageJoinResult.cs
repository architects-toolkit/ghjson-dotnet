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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.MergeOperations
{
    /// <summary>
    /// Represents the result of joining multiple paginated GhJSON documents into a single document.
    /// </summary>
    public sealed class PageJoinResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageJoinResult"/> class.
        /// </summary>
        public PageJoinResult()
        {
            this.Conflicts = new List<string>();
        }

        /// <summary>
        /// Gets or sets the joined document.
        /// </summary>
        public GhJsonDocument Document { get; set; } = new GhJsonDocument();

        /// <summary>
        /// Gets or sets a value indicating whether the join was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the list of conflicts or warnings that occurred during the join.
        /// </summary>
        public List<string> Conflicts { get; set; }

        /// <summary>
        /// Gets or sets the number of duplicate components removed during the join.
        /// </summary>
        public int DuplicateComponentsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the number of boundary connections that became internal after joining.
        /// </summary>
        public int BoundaryConnectionsResolved { get; set; }

        /// <summary>
        /// Gets or sets the number of dangling non-boundary connections removed during the join.
        /// </summary>
        public int DanglingConnectionsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the number of empty groups removed during the join.
        /// </summary>
        public int EmptyGroupsRemoved { get; set; }
    }
}
