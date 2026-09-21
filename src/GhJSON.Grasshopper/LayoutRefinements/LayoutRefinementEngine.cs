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

namespace GhJSON.Grasshopper.LayoutRefinements
{
    public static class LayoutRefinementEngine
    {
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
                    options.NodeSizeProvider);
            }

            // Port alignment and collision resolution compete: alignment pulls sources
            // toward exact port heights while collision pushes overlapping column members
            // down, silently re-breaking alignment. Iterate the pair — collision always
            // gets the final word within a pass — until positions converge or the pass
            // budget runs out. Any of the legacy alignment flags enables the pass.
            var alignToPorts = options.AlignParamsToInputPorts ||
                               options.AlignOneToOneConnections ||
                               options.MinimizeConnectionLengths;

            if (alignToPorts || options.AvoidCollisions)
            {
                const int maxPasses = 3;
                const float convergenceEpsilon = 0.5f;

                for (var pass = 0; pass < maxPasses; pass++)
                {
                    var before = positions;

                    if (alignToPorts)
                    {
                        positions = PortAlignment.AlignToPorts(positions, document);
                    }

                    if (options.AvoidCollisions)
                    {
                        positions = CollisionResolver.AvoidCollisions(positions, options.NodeSizeProvider);
                    }

                    if (MaxMovement(before, positions) <= convergenceEpsilon)
                    {
                        break;
                    }
                }
            }

            return positions;
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
