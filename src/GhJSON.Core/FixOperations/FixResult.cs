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

namespace GhJSON.Core.FixOperations
{
    /// <summary>
    /// Represents the result of a fix operation on a GhJSON document.
    /// </summary>
    public sealed class FixResult
    {
        /// <summary>
        /// Gets or sets the fixed document.
        /// </summary>
        public GhJsonDocument Document { get; set; } = new GhJsonDocument();

        /// <summary>
        /// Gets or sets a value indicating whether any changes were made.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Gets or sets the list of actions that were applied.
        /// </summary>
        public List<string> AppliedActions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of issues that could not be fixed.
        /// </summary>
        public List<string> UnfixedIssues { get; set; } = new List<string>();
    }
}
