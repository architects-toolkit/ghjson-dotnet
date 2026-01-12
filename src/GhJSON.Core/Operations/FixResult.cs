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
using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations
{
    /// <summary>
    /// Result of applying fix operations to a GhJSON document.
    /// </summary>
    public class FixResult
    {
        /// <summary>
        /// Gets the fixed document.
        /// </summary>
        public GhJsonDocument Document { get; }

        /// <summary>
        /// Gets the list of fix actions that were applied.
        /// </summary>
        public List<FixAction> Actions { get; } = new List<FixAction>();

        /// <summary>
        /// Gets whether any modifications were made.
        /// </summary>
        public bool WasModified => Actions.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixResult"/> class.
        /// </summary>
        /// <param name="document">The fixed document.</param>
        public FixResult(GhJsonDocument document)
        {
            Document = document;
        }
    }

    /// <summary>
    /// Describes a fix action that was applied to a document.
    /// </summary>
    public class FixAction
    {
        /// <summary>
        /// Gets or sets the type of fix action.
        /// </summary>
        public FixActionType Type { get; set; }

        /// <summary>
        /// Gets or sets a description of what was fixed.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of items affected by this fix.
        /// </summary>
        public int ItemsAffected { get; set; }

        /// <summary>
        /// Creates a new fix action.
        /// </summary>
        public static FixAction Create(FixActionType type, string description, int itemsAffected) =>
            new FixAction
            {
                Type = type,
                Description = description,
                ItemsAffected = itemsAffected
            };
    }

    /// <summary>
    /// Types of fix actions.
    /// </summary>
    public enum FixActionType
    {
        /// <summary>IDs were assigned to components.</summary>
        IdsAssigned,

        /// <summary>Instance GUIDs were generated.</summary>
        GuidsGenerated,

        /// <summary>Metadata was populated.</summary>
        MetadataPopulated,

        /// <summary>Counts were updated.</summary>
        CountsUpdated,

        /// <summary>Connections were validated/fixed.</summary>
        ConnectionsValidated,

        /// <summary>Other fix action.</summary>
        Other
    }
}
