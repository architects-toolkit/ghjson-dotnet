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

namespace GhJSON.Grasshopper.Deserialization
{
    /// <summary>
    /// Result of deserializing a GhJSON document to Grasshopper objects.
    /// </summary>
    public sealed class DeserializationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether deserialization was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the deserialized document objects (not yet placed on canvas).
        /// </summary>
        public List<IGH_DocumentObject> Objects { get; set; } = new List<IGH_DocumentObject>();

        /// <summary>
        /// Gets or sets the mapping of component IDs to created objects.
        /// </summary>
        public Dictionary<int, IGH_DocumentObject> IdToObjectMapping { get; set; } = new Dictionary<int, IGH_DocumentObject>();

        /// <summary>
        /// Gets or sets the list of components that failed to deserialize.
        /// </summary>
        public List<string> FailedComponents { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of warning messages.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the error message if deserialization failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
