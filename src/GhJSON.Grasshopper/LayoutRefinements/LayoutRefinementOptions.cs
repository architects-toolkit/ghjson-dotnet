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

using System;
using System.Drawing;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    public sealed class LayoutRefinementOptions
    {
        public bool ApplyBoundsAwareSpacing { get; set; } = true;

        /// <summary>
        /// Optional node-size lookup matching <see cref="GhJSON.Core.DependencyGraph.LayoutOptions.NodeSizeProvider"/>.
        /// Consulted before live canvas bounds so refinements can measure objects that are
        /// instantiated but not yet added to the document (e.g. the gh_put flow).
        /// </summary>
        public Func<Guid, SizeF?>? NodeSizeProvider { get; set; }

        public bool AlignParamsToInputPorts { get; set; } = true;

        public bool AlignOneToOneConnections { get; set; } = true;

        public bool MinimizeConnectionLengths { get; set; } = true;

        public bool AvoidCollisions { get; set; } = true;

        /// <summary>
        /// Horizontal gap between a column's right edge and the next column's left edge.
        /// Mirrors <see cref="GhJSON.Core.DependencyGraph.LayoutOptions.SpacingX"/>.
        /// </summary>
        public float SpacingX { get; set; } = 80f;

        /// <summary>
        /// Vertical gap between consecutive rows: the bottom edge of a row's tallest node
        /// to the top edge of the next row's nodes.
        /// </summary>
        public float SpacingY { get; set; } = 28f;

        public static LayoutRefinementOptions Default => new LayoutRefinementOptions();

        public static LayoutRefinementOptions None => new LayoutRefinementOptions
        {
            ApplyBoundsAwareSpacing = false,
            AlignParamsToInputPorts = false,
            AlignOneToOneConnections = false,
            MinimizeConnectionLengths = false,
            AvoidCollisions = false,
        };
    }
}
