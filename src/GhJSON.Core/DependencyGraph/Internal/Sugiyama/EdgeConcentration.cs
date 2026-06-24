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

namespace GhJSON.Core.DependencyGraph.Internal.Sugiyama
{
    /// <summary>
    /// Splits every edge that spans more than one layer into a chain of synthetic "dummy"
    /// routing nodes, one per intermediate layer. This is the standard Sugiyama device that
    /// lets the ordering / crossing-minimization passes reason about long edges the same way
    /// as short ones, producing straighter wires and meaningful crossing counts. Dummy nodes
    /// are flagged via <see cref="LayoutNode.IsDummy"/> and are never emitted as real
    /// component positions.
    /// </summary>
    internal static class EdgeConcentration
    {
        public static List<LayoutNode> InsertDummyChains(List<LayoutNode> nodes, LayoutOptions options)
        {
            var byId = nodes.ToDictionary(n => n.ComponentId, n => n);
            var result = new List<LayoutNode>(nodes);

            // Snapshot edges first so the adjacency can be mutated while iterating.
            var edges = new List<(Guid From, Guid To, int ParamIndex)>();
            foreach (var u in nodes)
            {
                foreach (var kv in u.Children)
                {
                    if (byId.ContainsKey(kv.Key))
                    {
                        edges.Add((u.ComponentId, kv.Key, kv.Value));
                    }
                }
            }

            var dummyWidth = options.DefaultNodeWidth * 0.1f;
            var dummyHeight = options.DefaultNodeHeight * 0.1f;

            foreach (var (fromId, toId, paramIndex) in edges)
            {
                var from = byId[fromId];
                var to = byId[toId];
                var span = to.Layer - from.Layer;
                if (span <= 1)
                {
                    // Same-layer (cycle) or adjacent edges need no routing nodes.
                    continue;
                }

                // Drop the direct edge; it is replaced by the dummy chain.
                from.Children.Remove(toId);
                to.Parents.Remove(fromId);

                var prevId = fromId;
                for (var lyr = from.Layer + 1; lyr < to.Layer; lyr++)
                {
                    var dummy = new LayoutNode
                    {
                        ComponentId = Guid.NewGuid(),
                        IsDummy = true,
                        Layer = lyr,
                        Width = dummyWidth,
                        Height = dummyHeight,
                        Parents = new Dictionary<Guid, int>(),
                        Children = new Dictionary<Guid, int>(),
                    };

                    result.Add(dummy);
                    byId[dummy.ComponentId] = dummy;

                    byId[prevId].Children[dummy.ComponentId] = -1;
                    dummy.Parents[prevId] = -1;
                    prevId = dummy.ComponentId;
                }

                // Re-attach the final segment to the real target, preserving the param index.
                byId[prevId].Children[toId] = paramIndex;
                to.Parents[prevId] = paramIndex;
            }

            return result;
        }
    }
}
