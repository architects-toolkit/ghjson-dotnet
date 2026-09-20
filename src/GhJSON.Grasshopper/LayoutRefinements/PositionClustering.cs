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
using System.Linq;

namespace GhJSON.Grasshopper.LayoutRefinements
{
    /// <summary>
    /// Groups layout positions into columns or rows by proximity along one axis. Replaces
    /// truncation/rounding keys, which fragmented groups whenever upstream passes introduced
    /// sub-pixel drift.
    /// </summary>
    internal static class PositionClustering
    {
        /// <summary>Positions closer than this many pixels share a cluster.</summary>
        public const float DefaultTolerance = 1f;

        /// <summary>
        /// Sorts <paramref name="positions"/> by the axis value and starts a new cluster
        /// whenever consecutive values differ by more than <paramref name="tolerance"/>.
        /// Returns clusters ordered by their ascending axis values.
        /// </summary>
        /// <param name="positions">Position entries to group.</param>
        /// <param name="axis">Axis projection, e.g. <c>p =&gt; p.X</c> for columns.</param>
        /// <param name="tolerance">Maximum gap between consecutive members of a cluster.</param>
        public static List<List<KeyValuePair<Guid, PointF>>> Cluster(
            IEnumerable<KeyValuePair<Guid, PointF>> positions,
            Func<PointF, float> axis,
            float tolerance = DefaultTolerance)
        {
            var sorted = positions.OrderBy(kvp => axis(kvp.Value)).ToList();
            var clusters = new List<List<KeyValuePair<Guid, PointF>>>();

            foreach (var kvp in sorted)
            {
                var last = clusters.Count > 0 ? clusters[clusters.Count - 1] : null;
                if (last != null &&
                    axis(kvp.Value) - axis(last[last.Count - 1].Value) <= tolerance)
                {
                    last.Add(kvp);
                }
                else
                {
                    clusters.Add(new List<KeyValuePair<Guid, PointF>> { kvp });
                }
            }

            return clusters;
        }
    }
}
