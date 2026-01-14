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

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Result of placing objects on the Grasshopper canvas.
    /// </summary>
    public sealed class PutResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether placement was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of components placed.
        /// </summary>
        public int ComponentsPlaced { get; set; }

        /// <summary>
        /// Gets or sets the number of connections created.
        /// </summary>
        public int ConnectionsCreated { get; set; }

        /// <summary>
        /// Gets or sets the number of groups created.
        /// </summary>
        public int GroupsCreated { get; set; }

        /// <summary>
        /// Gets or sets the placed document objects.
        /// </summary>
        public List<IGH_DocumentObject> PlacedObjects { get; set; } = new List<IGH_DocumentObject>();

        /// <summary>
        /// Gets or sets the mapping of component IDs to placed object GUIDs.
        /// </summary>
        public Dictionary<int, Guid> IdToGuidMapping { get; set; } = new Dictionary<int, Guid>();

        /// <summary>
        /// Gets or sets the list of components that failed to place.
        /// </summary>
        public List<string> FailedComponents { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of warning messages.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the error message if placement failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
