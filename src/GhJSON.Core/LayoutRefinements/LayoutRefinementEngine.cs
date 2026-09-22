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
using System.Collections.Generic;
using System.Drawing;
using GhJSON.Core.DependencyGraph;
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Runs the measurement-aware refinement passes over a core
    /// <see cref="LayoutResult"/>: per-island bounds-aware spacing first, then an
    /// iterated wire-clearance/port-alignment/collision loop that converges to whole
    /// pixels. The engine is host-independent — it consumes measured node geometry
    /// through <see cref="LayoutRefinementOptions.NodeMetricsProvider"/> — so the same
    /// pipeline runs on a live Grasshopper canvas (via
    /// <c>GhJsonGrasshopper.ComputeLayout</c>) or on any other adapter that supplies
    /// metrics.
    /// </summary>
    public static class LayoutRefinementEngine
    {
        /// <summary>
        /// Applies the enabled refinement passes and returns refined bounds-center
        /// positions per layout key.
        /// </summary>
        /// <param name="layoutResult">Core dependency-graph layout output.</param>
        /// <param name="document">The GhJSON document supplying connections and components.</param>
        /// <param name="options">Refinement toggles and providers; null uses defaults.</param>
        public static Dictionary<Guid, PointF> ApplyRefinements(
            LayoutResult layoutResult,
            GhJsonDocument document,
            LayoutRefinementOptions? options = null)
        {
            if (layoutResult == null)
            {
                throw new ArgumentNullException(nameof(layoutResult));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            options ??= LayoutRefinementOptions.Default;

            var positions = new Dictionary<Guid, PointF>();
            foreach (var kvp in layoutResult.Positions)
            {
                positions[kvp.Key] = new PointF((float)kvp.Value.X, (float)kvp.Value.Y);
            }

            if (options.ApplyBoundsAwareSpacing)
            {
                positions = BoundsAwareSpacing.ApplyBoundsAwareSpacing(
                    positions,
                    options.SpacingX,
                    options.SpacingY,
                    layoutResult.Islands,
                    options.NodeMetricsProvider);
            }

            // Port alignment, wire corridors, and collision resolution compete: corridor
            // planning and alignment pull nodes toward wire-friendly heights while
            // collision pushes overlapping column members down, silently re-breaking
            // both. Iterate the trio — collision always gets the final word within a
            // pass — until positions converge or the pass budget runs out. Positions
            // round to whole pixels each pass so sub-pixel oscillation cannot stall
            // convergence or leak ±1px drift into undo entries.
            if (options.AlignToPorts || options.AvoidCollisions || options.EnsureWireClearance)
            {
                const int maxPasses = 3;
                const float convergenceEpsilon = 0.5f;

                for (var pass = 0; pass < maxPasses; pass++)
                {
                    var before = positions;

                    if (options.EnsureWireClearance)
                    {
                        positions = WireClearance.EnsureWireClearance(
                            positions,
                            document,
                            layoutResult.Islands,
                            options.NodeMetricsProvider,
                            options.WireClearance);
                    }

                    if (options.AlignToPorts)
                    {
                        positions = PortAlignment.AlignToPorts(positions, document, options.NodeMetricsProvider);
                    }

                    if (options.AvoidCollisions)
                    {
                        positions = CollisionResolver.AvoidCollisions(
                            positions,
                            options.NodeMetricsProvider);
                    }

                    positions = Round(positions);

                    if (MaxMovement(before, positions) <= convergenceEpsilon)
                    {
                        break;
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Snaps positions to whole pixels. Grasshopper draws on a pixel grid, so the
        /// sub-pixel residue left by median/port arithmetic only produces undo noise.
        /// </summary>
        private static Dictionary<Guid, PointF> Round(Dictionary<Guid, PointF> positions)
        {
            var rounded = new Dictionary<Guid, PointF>(positions.Count);
            foreach (var kvp in positions)
            {
                rounded[kvp.Key] = new PointF(
                    (float)Math.Round(kvp.Value.X),
                    (float)Math.Round(kvp.Value.Y));
            }

            return rounded;
        }

        /// <summary>
        /// Largest Manhattan distance any component moved between two refinement passes.
        /// </summary>
        private static float MaxMovement(
            Dictionary<Guid, PointF> before,
            Dictionary<Guid, PointF> after)
        {
            var max = 0f;
            foreach (var kvp in after)
            {
                if (before.TryGetValue(kvp.Key, out var old))
                {
                    var delta = Math.Abs(kvp.Value.X - old.X) + Math.Abs(kvp.Value.Y - old.Y);
                    if (delta > max)
                    {
                        max = delta;
                    }
                }
            }

            return max;
        }
    }
}
