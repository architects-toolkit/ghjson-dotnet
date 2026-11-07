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
using GhJSON.Core.SchemaModels;

namespace GhJSON.Core.LayoutRefinements
{
    /// <summary>
    /// Fits a compact layout to fixed bounds centers without moving authored objects.
    /// Single-anchor islands translate rigidly; multiple anchors blend translations
    /// by inverse graph distance so nearby fixed neighbors have the greatest influence.
    /// </summary>
    internal static class AnchoredPlacement
    {
        /// <summary>Fits candidate bounds centers to the fixed graph boundaries.</summary>
        public static Dictionary<Guid, PointF> Apply(
            Dictionary<Guid, PointF> positions,
            GhJsonDocument document,
            LayoutRefinementOptions options)
        {
            var fixedPositions = options.FixedPositions!;
            var result = new Dictionary<Guid, PointF>(positions);
            var neighbors = positions.Keys.ToDictionary(id => id, _ => new HashSet<Guid>());
            var keys = ConnectionKeyMap.Build(document);
            foreach (var edge in document.Connections ?? Enumerable.Empty<GhJsonConnection>())
            {
                if (keys.TryGetValue(edge.From.Id, out var from) &&
                    keys.TryGetValue(edge.To.Id, out var to) &&
                    neighbors.ContainsKey(from) && neighbors.ContainsKey(to))
                {
                    neighbors[from].Add(to);
                    neighbors[to].Add(from);
                }
            }

            var unvisited = new HashSet<Guid>(positions.Keys);
            var unanchored = new List<Guid>();
            foreach (var seed in positions.Keys.OrderBy(id => id))
            {
                if (!unvisited.Contains(seed))
                {
                    continue;
                }

                var members = Distances(seed, neighbors).Keys.OrderBy(id => id).ToList();
                unvisited.ExceptWith(members);
                var anchors = members.Where(fixedPositions.ContainsKey).ToList();
                if (anchors.Count == 0)
                {
                    unanchored.AddRange(members);
                    continue;
                }

                var distances = anchors.ToDictionary(id => id, id => Distances(id, neighbors, fixedPositions));
                foreach (var id in members)
                {
                    if (fixedPositions.TryGetValue(id, out var fixedPosition))
                    {
                        result[id] = fixedPosition;
                        continue;
                    }

                    double sumX = 0, sumY = 0, totalWeight = 0;
                    foreach (var anchor in anchors)
                    {
                        if (!distances[anchor].TryGetValue(id, out var distance))
                        {
                            continue;
                        }

                        var weight = 1d / distance;
                        sumX += (fixedPositions[anchor].X - positions[anchor].X) * weight;
                        sumY += (fixedPositions[anchor].Y - positions[anchor].Y) * weight;
                        totalWeight += weight;
                    }

                    result[id] = new PointF(
                        positions[id].X + (float)(sumX / totalWeight),
                        positions[id].Y + (float)(sumY / totalWeight));
                }
            }

            if (unanchored.Count > 0)
            {
                var minTop = unanchored.Min(id => positions[id].Y - (Size(id, options).Height / 2f));
                foreach (var id in unanchored)
                {
                    result[id] = new PointF(
                        positions[id].X + options.UnanchoredOffset.X,
                        positions[id].Y + options.UnanchoredOffset.Y - minTop);
                }
            }

            if (!options.AvoidCollisions)
            {
                return result;
            }

            // Fixed rectangles are obstacles. Missing-position nodes take the nearest
            // free vertical slot; interval endpoints avoid iterative push/pull cycles.
            var blockers = result.Keys.Where(fixedPositions.ContainsKey)
                .Select(id => Bounds(result[id], Size(id, options))).ToList();
            var unanchoredSet = new HashSet<Guid>(unanchored);
            foreach (var id in result.Keys.Where(id => !fixedPositions.ContainsKey(id))
                         .OrderBy(unanchoredSet.Contains)
                         .ThenBy(id => result[id].X).ThenBy(id => result[id].Y).ThenBy(id => id).ToList())
            {
                var point = result[id];
                var size = Size(id, options);
                // Core center rounding can shorten an edge gap by up to one pixel.
                var intervals = blockers
                    .Where(b => Math.Abs(point.X - (b.Left + (b.Width / 2f))) <
                                ((size.Width + b.Width) / 2f) + options.SpacingX - 1f)
                    .Select(b => (Top: b.Top - (size.Height / 2f) - options.SpacingY,
                                  Bottom: b.Bottom + (size.Height / 2f) + options.SpacingY))
                    .OrderBy(i => i.Top).ToList();
                var merged = new List<(float Top, float Bottom)>();
                foreach (var interval in intervals)
                {
                    if (merged.Count > 0 && interval.Top < merged[merged.Count - 1].Bottom)
                    {
                        var last = merged[merged.Count - 1];
                        merged[merged.Count - 1] = (last.Top, Math.Max(last.Bottom, interval.Bottom));
                    }
                    else
                    {
                        merged.Add(interval);
                    }
                }

                var y = point.Y;
                foreach (var interval in merged)
                {
                    if (y > interval.Top && y < interval.Bottom)
                    {
                        y = y - interval.Top <= interval.Bottom - y ? interval.Top : interval.Bottom;
                        break;
                    }
                }
                result[id] = new PointF(point.X, y);
                blockers.Add(Bounds(result[id], size));
            }

            return result;
        }

        /// <summary>
        /// Measures undirected graph distance, optionally stopping at other fixed
        /// nodes so distant anchors cannot pull nodes across a nearer fixed boundary.
        /// </summary>
        private static Dictionary<Guid, int> Distances(
            Guid start,
            Dictionary<Guid, HashSet<Guid>> neighbors,
            IReadOnlyDictionary<Guid, PointF>? fixedPositions = null)
        {
            var distances = new Dictionary<Guid, int> { [start] = 0 };
            var queue = new Queue<Guid>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current != start && fixedPositions?.ContainsKey(current) == true)
                {
                    continue;
                }

                foreach (var neighbor in neighbors[current].OrderBy(id => id))
                {
                    if (!distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = distances[current] + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return distances;
        }

        /// <summary>Resolves measured bounds or the core layout's fallback size.</summary>
        private static SizeF Size(Guid id, LayoutRefinementOptions options)
        {
            return LayoutNodeMetrics.Resolve(options.NodeMetricsProvider, id)?.Size ?? new SizeF(100f, 60f);
        }

        /// <summary>Converts a bounds center and size to an obstacle rectangle.</summary>
        private static RectangleF Bounds(PointF center, SizeF size)
        {
            return new RectangleF(center.X - (size.Width / 2f), center.Y - (size.Height / 2f), size.Width, size.Height);
        }
    }
}
