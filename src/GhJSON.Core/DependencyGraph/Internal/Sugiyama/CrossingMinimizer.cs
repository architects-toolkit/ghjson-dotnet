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
using System.Linq;

namespace GhJSON.Core.DependencyGraph.Internal.Sugiyama
{
    /// <summary>
    /// Refines the within-layer ordering (<see cref="LayoutNode.Order"/>) to minimize wire
    /// crossings. Each iteration performs a weighted-median sweep (alternating down/up)
    /// followed by a transpose pass (adjacent swaps that strictly reduce crossings). The
    /// total crossing count is measured after every iteration and the best ordering seen is
    /// retained, so increasing <see cref="LayoutOptions.MaxOrderingIterations"/> can never
    /// yield a worse result. All neighbor lookups go through per-layer order maps, keeping
    /// each sweep near-linear in the number of edges.
    /// </summary>
    internal static class CrossingMinimizer
    {
        public static void MinimizeCrossings(List<LayoutNode> nodes, int maxIterations = 24)
        {
            var layers = BuildLayers(nodes);
            if (layers.Count < 2)
            {
                return; // Nothing to cross.
            }

            var bestOrder = SnapshotOrder(nodes);
            var bestCrossings = CountAllCrossings(layers);

            for (var iter = 0; iter < maxIterations && bestCrossings > 0; iter++)
            {
                var down = iter % 2 == 0;
                MedianSweep(layers, down);
                Transpose(layers);

                var crossings = CountAllCrossings(layers);
                if (crossings < bestCrossings)
                {
                    bestCrossings = crossings;
                    bestOrder = SnapshotOrder(nodes);
                }
            }

            RestoreOrder(nodes, bestOrder);
        }

        /// <summary>
        /// Counts the total number of edge crossings in the current ordering. Exposed so the
        /// layout engine can report it as a diagnostic / quality metric.
        /// </summary>
        public static int CountCrossings(List<LayoutNode> nodes)
        {
            return CountAllCrossings(BuildLayers(nodes));
        }

        private static List<List<LayoutNode>> BuildLayers(List<LayoutNode> nodes)
        {
            return nodes.GroupBy(n => n.Layer)
                        .OrderBy(g => g.Key)
                        .Select(g => g.OrderBy(n => n.Order).ToList())
                        .ToList();
        }

        private static void MedianSweep(List<List<LayoutNode>> layers, bool down)
        {
            if (down)
            {
                for (var li = 1; li < layers.Count; li++)
                {
                    ReorderLayer(layers[li], BuildOrderLookup(layers[li - 1]), useParents: true);
                }
            }
            else
            {
                for (var li = layers.Count - 2; li >= 0; li--)
                {
                    ReorderLayer(layers[li], BuildOrderLookup(layers[li + 1]), useParents: false);
                }
            }
        }

        /// <summary>
        /// Sorts <paramref name="layer"/> by each node's median neighbor position. Nodes with
        /// no neighbor in the adjacent layer are pinned to their current position (classic
        /// Sugiyama behavior) rather than being swept to one end.
        /// </summary>
        private static void ReorderLayer(List<LayoutNode> layer, Dictionary<Guid, int> adjacentOrder, bool useParents)
        {
            // Capture fixed nodes' slots so they stay put; sort the rest by median.
            var medians = new Dictionary<Guid, float>(layer.Count);
            var movable = new List<LayoutNode>();
            foreach (var node in layer)
            {
                var median = Median(node, adjacentOrder, useParents);
                if (median < 0f)
                {
                    medians[node.ComponentId] = -1f; // fixed
                }
                else
                {
                    medians[node.ComponentId] = median;
                    movable.Add(node);
                }
            }

            if (movable.Count == 0)
            {
                return;
            }

            // Sort movable nodes by median (stable on ComponentId), then re-thread them back
            // into the slots not occupied by fixed nodes, preserving fixed node positions.
            movable.Sort((a, b) =>
            {
                var cmp = medians[a.ComponentId].CompareTo(medians[b.ComponentId]);
                return cmp != 0 ? cmp : a.ComponentId.CompareTo(b.ComponentId);
            });

            var result = new LayoutNode[layer.Count];
            var fixedSlots = new HashSet<int>();
            for (var i = 0; i < layer.Count; i++)
            {
                if (medians[layer[i].ComponentId] < 0f)
                {
                    result[i] = layer[i];
                    fixedSlots.Add(i);
                }
            }

            var m = 0;
            for (var i = 0; i < layer.Count; i++)
            {
                if (fixedSlots.Contains(i))
                {
                    continue;
                }

                result[i] = movable[m++];
            }

            for (var i = 0; i < layer.Count; i++)
            {
                layer[i] = result[i];
                layer[i].Order = i;
            }
        }

        /// <summary>
        /// Transpose heuristic: repeatedly swap adjacent nodes within a layer when doing so
        /// reduces the combined crossings with the neighboring layers, until no swap helps.
        /// </summary>
        private static void Transpose(List<List<LayoutNode>> layers)
        {
            var improved = true;
            var guard = 0;
            while (improved && guard++ < 4)
            {
                improved = false;
                for (var li = 0; li < layers.Count; li++)
                {
                    var layer = layers[li];
                    for (var i = 0; i < layer.Count - 1; i++)
                    {
                        var before = LocalCrossings(layers, li);
                        Swap(layer, i, i + 1);
                        var after = LocalCrossings(layers, li);
                        if (after < before)
                        {
                            improved = true;
                        }
                        else
                        {
                            Swap(layer, i, i + 1); // revert
                        }
                    }
                }
            }
        }

        private static void Swap(List<LayoutNode> layer, int i, int j)
        {
            (layer[i], layer[j]) = (layer[j], layer[i]);
            layer[i].Order = i;
            layer[j].Order = j;
        }

        private static int LocalCrossings(List<List<LayoutNode>> layers, int layerIndex)
        {
            var total = 0;
            if (layerIndex > 0)
            {
                total += CountCrossingsBetween(layers[layerIndex - 1], layers[layerIndex]);
            }

            if (layerIndex < layers.Count - 1)
            {
                total += CountCrossingsBetween(layers[layerIndex], layers[layerIndex + 1]);
            }

            return total;
        }

        private static int CountAllCrossings(List<List<LayoutNode>> layers)
        {
            var total = 0;
            for (var li = 0; li < layers.Count - 1; li++)
            {
                total += CountCrossingsBetween(layers[li], layers[li + 1]);
            }

            return total;
        }

        /// <summary>
        /// Counts crossings between two adjacent layers by collecting all edges as
        /// (upperOrder, lowerOrder) pairs and counting inversions.
        /// </summary>
        private static int CountCrossingsBetween(List<LayoutNode> upper, List<LayoutNode> lower)
        {
            var lowerOrder = BuildOrderLookup(lower);
            var edges = new List<(int Upper, int Lower)>();
            foreach (var u in upper)
            {
                foreach (var childId in u.Children.Keys)
                {
                    if (lowerOrder.TryGetValue(childId, out var lo))
                    {
                        edges.Add((u.Order, lo));
                    }
                }
            }

            // Sort by upper position, then count inversions in the lower positions.
            edges.Sort((a, b) => a.Upper != b.Upper ? a.Upper.CompareTo(b.Upper) : a.Lower.CompareTo(b.Lower));

            var crossings = 0;
            for (var i = 0; i < edges.Count; i++)
            {
                for (var j = i + 1; j < edges.Count; j++)
                {
                    if (edges[j].Lower < edges[i].Lower)
                    {
                        crossings++;
                    }
                }
            }

            return crossings;
        }

        private static Dictionary<Guid, int> BuildOrderLookup(List<LayoutNode> layer)
        {
            var map = new Dictionary<Guid, int>(layer.Count);
            foreach (var n in layer)
            {
                map[n.ComponentId] = n.Order;
            }

            return map;
        }

        /// <summary>
        /// Weighted median of a node's neighbor positions in the adjacent layer. Returns -1
        /// when the node has no resolvable neighbor (treated as "fixed in place").
        /// </summary>
        private static float Median(LayoutNode node, Dictionary<Guid, int> adjacentOrder, bool useParents)
        {
            var connected = useParents ? node.Parents.Keys : node.Children.Keys;
            var positions = new List<int>();
            foreach (var id in connected)
            {
                if (adjacentOrder.TryGetValue(id, out var order))
                {
                    positions.Add(order);
                }
            }

            if (positions.Count == 0)
            {
                return -1f;
            }

            positions.Sort();
            var mid = positions.Count / 2;
            if (positions.Count % 2 == 1)
            {
                return positions[mid];
            }

            return (positions[mid - 1] + positions[mid]) / 2f;
        }

        private static Dictionary<Guid, int> SnapshotOrder(List<LayoutNode> nodes)
        {
            var snapshot = new Dictionary<Guid, int>(nodes.Count);
            foreach (var n in nodes)
            {
                snapshot[n.ComponentId] = n.Order;
            }

            return snapshot;
        }

        private static void RestoreOrder(List<LayoutNode> nodes, Dictionary<Guid, int> order)
        {
            foreach (var n in nodes)
            {
                if (order.TryGetValue(n.ComponentId, out var o))
                {
                    n.Order = o;
                }
            }
        }
    }
}
