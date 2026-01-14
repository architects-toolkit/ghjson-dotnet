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

using System.Drawing;

namespace GhJSON.Grasshopper.PutOperations
{
    /// <summary>
    /// Options for placing objects on the Grasshopper canvas.
    /// </summary>
    public sealed class PutOptions
    {
        /// <summary>
        /// Gets the default put options.
        /// </summary>
        public static PutOptions Default { get; } = new PutOptions();

        /// <summary>
        /// Gets or sets the offset to apply to component positions.
        /// </summary>
        public PointF Offset { get; set; } = new PointF(0, 0);

        /// <summary>
        /// Gets or sets a value indicating whether to create connections.
        /// </summary>
        public bool CreateConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to create groups.
        /// </summary>
        public bool CreateGroups { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to select placed objects.
        /// </summary>
        public bool SelectPlacedObjects { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to regenerate instance GUIDs.
        /// </summary>
        public bool RegenerateInstanceGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip invalid components.
        /// </summary>
        public bool SkipInvalidComponents { get; set; } = true;
    }
}
