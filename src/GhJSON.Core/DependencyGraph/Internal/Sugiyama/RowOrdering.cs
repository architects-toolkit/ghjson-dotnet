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
    /// Produces a sensible initial within-layer ordering (<see cref="LayoutNode.Order"/>) via
    /// a single top-down barycenter pass seeded from a deterministic source ordering. The
    /// <see cref="CrossingMinimizer"/> then iteratively refines this ordering.
    /// </summary>
    internal static class RowOrdering
    {
        /// <summary>
        /// Assigns an initial <see cref="LayoutNode.Order"/> to every node, grouped by
        /// <see cref="LayoutNode.Layer"/>.
        /// </summary>
        public static void AssignInitialOrder(List<LayoutNode> nodes)
        {
            var byLayer = nodes.GroupBy(n => n.Layer)
                               .OrderBy(g => g.Key)
                               .Select(g => g.ToList())
                               .ToList();

            if (byLayer.Count == 0)
            {
                return;
            }

            // Layer 0: stable deterministic seed (by ComponentId) so output is reproducible.
            var first = byLayer[0];
            first.Sort((a, b) => a.ComponentId.CompareTo(b.ComponentId));
            AssignOrderIndices(first);

            // Subsequent layers: order by barycenter of already-placed parents.
            for (var li = 1; li < byLayer.Count; li++)
            {
                var prevOrder = BuildOrderLookup(byLayer[li - 1]);
                var current = byLayer[li];
                current.Sort((a, b) =>
                {
                    var cmp = Barycenter(a, prevOrder, useParents: true)
                        .CompareTo(Barycenter(b, prevOrder, useParents: true));
                    return cmp != 0 ? cmp : a.ComponentId.CompareTo(b.ComponentId);
                });
                AssignOrderIndices(current);
            }
        }

        private static void AssignOrderIndices(List<LayoutNode> layer)
        {
            for (var i = 0; i < layer.Count; i++)
            {
                layer[i].Order = i;
            }
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
        /// Average order index of a node's neighbors in the adjacent layer. Nodes with no
        /// resolvable neighbor return <see cref="float.MaxValue"/> so they sort to the end of
        /// the initial ordering (the crossing minimizer subsequently keeps them in place).
        /// </summary>
        private static float Barycenter(LayoutNode node, Dictionary<Guid, int> adjacentOrder, bool useParents)
        {
            var connected = useParents ? node.Parents.Keys : node.Children.Keys;
            var sum = 0f;
            var count = 0;
            foreach (var id in connected)
            {
                if (adjacentOrder.TryGetValue(id, out var order))
                {
                    sum += order;
                    count++;
                }
            }

            return count == 0 ? float.MaxValue : sum / count;
        }
    }
}
