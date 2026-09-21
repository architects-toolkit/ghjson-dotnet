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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DependencyGraph
{
    public sealed class LayoutOptions
    {
        public LayoutAlgorithm Algorithm { get; set; } = LayoutAlgorithm.Sugiyama;

        /// <summary>
        /// Horizontal gap between a column's right edge and the next column's left edge.
        /// </summary>
        public float SpacingX { get; set; } = 80f;

        /// <summary>
        /// Vertical gap between consecutive rows: the bottom edge of a row's tallest node
        /// to the top edge of the next row's nodes.
        /// </summary>
        public float SpacingY { get; set; } = 28f;

        /// <summary>
        /// Vertical gap between shelves when packing disconnected islands.
        /// </summary>
        public float IslandSpacingY { get; set; } = 100f;

        public bool PreserveIslandOrder { get; set; } = false;

        public GhJsonPivot? Origin { get; set; } = null;

        /// <summary>
        /// Optional provider of measured node bounds: layout node key (the component's
        /// instance GUID, or the synthesized key for id-only components) to its size in
        /// canvas units. Grasshopper consumers supply live <c>Attributes.Bounds</c> here so
        /// column widths and row heights reflect real component geometry instead of
        /// <see cref="DefaultNodeWidth"/>/<see cref="DefaultNodeHeight"/>. Return null (or a
        /// non-positive size) for unknown nodes to keep the defaults.
        /// </summary>
        public Func<Guid, SizeF?>? NodeSizeProvider { get; set; }

        /// <summary>
        /// Default component width used for bounds-aware column spacing when a node does not
        /// carry a measured width. Grasshopper consumers refine this with real canvas bounds.
        /// </summary>
        public float DefaultNodeWidth { get; set; } = 100f;

        /// <summary>
        /// Default component height used for bounds-aware row spacing when a node does not
        /// carry a measured height.
        /// </summary>
        public float DefaultNodeHeight { get; set; } = 60f;

        /// <summary>
        /// Maximum crossing-minimization iterations (down/up sweeps). The optimizer keeps the
        /// best ordering it has seen, so a higher cap never produces a worse result.
        /// </summary>
        public int MaxOrderingIterations { get; set; } = 24;

        public static LayoutOptions Default => new LayoutOptions();
    }
}
