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

using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.DependencyGraph
{
    public sealed class LayoutOptions
    {
        public LayoutAlgorithm Algorithm { get; set; } = LayoutAlgorithm.Sugiyama;

        public float SpacingX { get; set; } = 200f;

        public float SpacingY { get; set; } = 100f;

        public float IslandSpacingY { get; set; } = 150f;

        public bool PreserveIslandOrder { get; set; } = false;

        public GhJsonPivot? Origin { get; set; } = null;

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

        /// <summary>
        /// When packing multiple disconnected islands, islands are laid out left-to-right on
        /// "shelves" until this width budget is exceeded, then wrapped to a new shelf. This
        /// keeps many small islands from forming one tall vertical strip.
        /// </summary>
        public float IslandWrapWidth { get; set; } = 2000f;

        public static LayoutOptions Default => new LayoutOptions();
    }
}
