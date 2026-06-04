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
    /// Assigns each node to a layer (column) using the <em>longest path from a source</em>.
    /// Nodes with no incoming edges (true inputs) are pinned to layer 0, so the layout reads
    /// left-to-right in workflow order. Implemented with an iterative back-edge scan plus a
    /// Kahn-style topological longest-path pass, so arbitrarily deep chains never risk a
    /// <see cref="StackOverflowException"/> and cycles are detected and broken (the back edge
    /// is ignored for ranking) rather than causing infinite recursion.
    /// </summary>
    internal static class LayerAssignment
    {
        /// <summary>
        /// Assigns layers in-place to <paramref name="nodes"/> (writing <see cref="LayoutNode.Layer"/>).
        /// Any GUIDs participating in a cycle are returned via <paramref name="cycleNodes"/>;
        /// the offending back edge is excluded from ranking to avoid a deadlock.
        /// </summary>
        public static void AssignLayers(List<LayoutNode> nodes, out IReadOnlyList<Guid> cycleNodes)
        {
            var present = new HashSet<Guid>(nodes.Select(n => n.ComponentId));
            var cycles = new HashSet<Guid>();
            var backEdges = FindBackEdges(nodes, present, cycles);

            // Build forward (acyclic) adjacency and in-degrees, ignoring back edges.
            var forward = new Dictionary<Guid, List<Guid>>(nodes.Count);
            var inDegree = new Dictionary<Guid, int>(nodes.Count);
            foreach (var node in nodes)
            {
                forward[node.ComponentId] = new List<Guid>();
                if (!inDegree.ContainsKey(node.ComponentId))
                {
                    inDegree[node.ComponentId] = 0;
                }
            }

            foreach (var node in nodes)
            {
                foreach (var childId in node.Children.Keys)
                {
                    if (!present.Contains(childId))
                    {
                        continue; // Dangling edge — validation reports this separately.
                    }

                    if (backEdges.Contains((node.ComponentId, childId)))
                    {
                        continue;
                    }

                    forward[node.ComponentId].Add(childId);
                    inDegree[childId] = inDegree.TryGetValue(childId, out var d) ? d + 1 : 1;
                }
            }

            // Kahn topological pass computing the longest path from any source.
            var layer = new Dictionary<Guid, int>(nodes.Count);
            var queue = new Queue<Guid>();
            foreach (var node in nodes)
            {
                if (inDegree[node.ComponentId] == 0)
                {
                    layer[node.ComponentId] = 0;
                    queue.Enqueue(node.ComponentId);
                }
            }

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var currentLayer = layer[id];
                foreach (var childId in forward[id])
                {
                    var candidate = currentLayer + 1;
                    if (!layer.TryGetValue(childId, out var existing) || candidate > existing)
                    {
                        layer[childId] = candidate;
                    }

                    if (--inDegree[childId] == 0)
                    {
                        queue.Enqueue(childId);
                    }
                }
            }

            foreach (var node in nodes)
            {
                node.Layer = layer.TryGetValue(node.ComponentId, out var l) ? l : 0;
            }

            cycleNodes = cycles.Count == 0 ? Array.Empty<Guid>() : cycles.ToList();
        }

        /// <summary>
        /// Backwards-compatible overload that discards cycle diagnostics.
        /// </summary>
        public static void AssignLayers(List<LayoutNode> nodes)
        {
            AssignLayers(nodes, out _);
        }

        /// <summary>
        /// Iterative (stack-based) DFS that identifies back edges in the children graph.
        /// A back edge is one that points to a node currently on the DFS stack (gray),
        /// indicating a cycle. Both endpoints are recorded in <paramref name="cycles"/>.
        /// </summary>
        private static HashSet<(Guid From, Guid To)> FindBackEdges(
            List<LayoutNode> nodes,
            HashSet<Guid> present,
            HashSet<Guid> cycles)
        {
            var backEdges = new HashSet<(Guid, Guid)>();
            var color = new Dictionary<Guid, byte>(nodes.Count); // 0=white,1=gray,2=black
            var childrenById = nodes.ToDictionary(n => n.ComponentId, n => n.Children);

            foreach (var root in nodes)
            {
                if (color.TryGetValue(root.ComponentId, out var c) && c != 0)
                {
                    continue;
                }

                var stack = new Stack<(Guid Node, IEnumerator<KeyValuePair<Guid, int>> Children)>();
                stack.Push((root.ComponentId, childrenById[root.ComponentId].GetEnumerator()));
                color[root.ComponentId] = 1;

                while (stack.Count > 0)
                {
                    var frame = stack.Peek();
                    if (frame.Children.MoveNext())
                    {
                        var childId = frame.Children.Current.Key;
                        if (!present.Contains(childId))
                        {
                            continue;
                        }

                        color.TryGetValue(childId, out var childColor);
                        if (childColor == 1)
                        {
                            // Edge into a node on the active stack → back edge / cycle.
                            backEdges.Add((frame.Node, childId));
                            cycles.Add(frame.Node);
                            cycles.Add(childId);
                        }
                        else if (childColor == 0)
                        {
                            color[childId] = 1;
                            stack.Push((childId, childrenById[childId].GetEnumerator()));
                        }
                    }
                    else
                    {
                        color[frame.Node] = 2;
                        stack.Pop();
                    }
                }
            }

            return backEdges;
        }
    }
}
