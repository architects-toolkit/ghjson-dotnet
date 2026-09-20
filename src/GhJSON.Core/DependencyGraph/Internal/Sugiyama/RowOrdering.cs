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
                var prevById = BuildNodeLookup(byLayer[li - 1]);
                var current = byLayer[li];
                current.Sort((a, b) =>
                {
                    var cmp = Barycenter(a, prevById, useParents: true)
                        .CompareTo(Barycenter(b, prevById, useParents: true));
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

        private static Dictionary<Guid, LayoutNode> BuildNodeLookup(List<LayoutNode> layer)
        {
            var map = new Dictionary<Guid, LayoutNode>(layer.Count);
            foreach (var n in layer)
            {
                map[n.ComponentId] = n;
            }

            return map;
        }

        /// <summary>
        /// Average port-aware position of a node's neighbors in the adjacent layer: each
        /// edge contributes the node order offset by the connected ports' fractional slot
        /// offsets, so a wire targets the slot where it enters/exits the port row rather
        /// than the neighbor's center. Nodes with no resolvable neighbor return
        /// <see cref="float.MaxValue"/> so they sort to the end of the initial ordering (the
        /// crossing minimizer subsequently keeps them in place).
        /// </summary>
        private static float Barycenter(LayoutNode node, Dictionary<Guid, LayoutNode> adjacent, bool useParents)
        {
            var sum = 0f;
            var count = 0;

            if (useParents)
            {
                foreach (var kv in node.Parents)
                {
                    if (adjacent.TryGetValue(kv.Key, out var parent))
                    {
                        var outIndex = parent.Children.TryGetValue(node.ComponentId, out var oi) ? oi : -1;
                        sum += parent.Order + parent.OutputPortCenterOffset(outIndex)
                             - node.InputPortCenterOffset(kv.Value);
                        count++;
                    }
                }
            }
            else
            {
                foreach (var kv in node.Children)
                {
                    if (adjacent.TryGetValue(kv.Key, out var child))
                    {
                        var inIndex = child.Parents.TryGetValue(node.ComponentId, out var ii) ? ii : -1;
                        sum += child.Order + child.InputPortCenterOffset(inIndex)
                             - node.OutputPortCenterOffset(kv.Value);
                        count++;
                    }
                }
            }

            return count == 0 ? float.MaxValue : sum / count;
        }
    }
}
