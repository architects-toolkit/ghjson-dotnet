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
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Options for the Grasshopper-aware refinement passes that run after the core
    /// dependency-graph layout: bounds-aware spacing, wire corridors, port alignment,
    /// and collision resolution. Each pass can be toggled independently.
    /// </summary>
    public sealed class LayoutRefinementOptions
    {
        /// <summary>
        /// Enables the per-island re-spacing pass that sizes each column and row from
        /// its members' measured bounds.
        /// </summary>
        public bool ApplyBoundsAwareSpacing { get; set; } = true;

        /// <summary>
        /// Optional node-size lookup matching <see cref="GhJSON.Core.DependencyGraph.LayoutOptions.NodeSizeProvider"/>.
        /// Consulted before live canvas bounds so refinements can measure objects that are
        /// instantiated but not yet added to the document (e.g. the gh_put flow).
        /// </summary>
        public Func<Guid, SizeF?>? NodeSizeProvider { get; set; }

        /// <summary>
        /// Optional object lookup resolving a layout key to its Grasshopper document
        /// object, supplied by the caller from its own object map (e.g. selected objects
        /// for tidy-up or freshly instantiated objects for gh_put). Consulted before the
        /// live document so refinements can read port geometry from objects that are not
        /// on the canvas yet. In-process only; never exposed through any external schema.
        /// </summary>
        public Func<Guid, IGH_DocumentObject?>? ObjectProvider { get; set; }

        /// <summary>
        /// Enables the port-alignment pass: each source is moved so its wire enters the
        /// target's specific input port horizontally, using real port bounds deltas.
        /// </summary>
        public bool AlignToPorts { get; set; } = true;

        /// <summary>
        /// Enables the collision-resolution pass that pushes overlapping column members
        /// apart vertically until their measured bounds no longer intersect.
        /// </summary>
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

        /// <summary>
        /// Reserves a clear corridor around wires that skip one or more columns so they
        /// do not run through unrelated component bounds. Intermediate nodes are nudged
        /// toward their nearer edge and skip-edge targets are lifted part-way, matching
        /// the manual tidy-up corrections users otherwise apply by hand.
        /// </summary>
        public bool EnsureWireClearance { get; set; } = true;

        /// <summary>
        /// Minimum required distance in pixels between a wire and the bounds of any
        /// component that is not one of its endpoints.
        /// </summary>
        public float WireClearance { get; set; } = 20f;

        public static LayoutRefinementOptions Default => new LayoutRefinementOptions();

        public static LayoutRefinementOptions None => new LayoutRefinementOptions
        {
            ApplyBoundsAwareSpacing = false,
            AlignToPorts = false,
            AvoidCollisions = false,
            EnsureWireClearance = false,
        };
    }
}
