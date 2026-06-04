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
    /// Assigns each node to a layer (column) based on its longest path to a leaf. Implemented
    /// as an iterative post-order DFS so arbitrarily deep dependency chains do not risk a
    /// <see cref="StackOverflowException"/>, and cyclic connections are detected and broken
    /// rather than causing infinite recursion.
    /// </summary>
    internal static class LayerAssignment
    {
        /// <summary>
        /// Assigns layers in-place to <paramref name="nodes"/>. Any GUIDs participating in a
        /// cycle (via <see cref="LayoutNode.Children"/>) are returned via
        /// <paramref name="cycleNodes"/>; their cycle edges are treated as same-layer to avoid
        /// a deadlock.
        /// </summary>
        public static void AssignLayers(List<LayoutNode> nodes, out IReadOnlyList<Guid> cycleNodes)
        {
            var graph = nodes.ToDictionary(n => n.ComponentId, n => n.Children);
            var layers = new Dictionary<Guid, int>(graph.Count);
            var state = new Dictionary<Guid, NodeState>(graph.Count);
            var cycles = new HashSet<Guid>();

            foreach (var rootId in graph.Keys)
            {
                if (state.TryGetValue(rootId, out var existing) && existing == NodeState.Done)
                {
                    continue;
                }

                VisitIterative(rootId, graph, layers, state, cycles);
            }

            var maxLayer = layers.Values.DefaultIfEmpty(0).Max();
            foreach (var node in nodes)
            {
                if (layers.TryGetValue(node.ComponentId, out var layer))
                {
                    node.Pivot = new PointF(maxLayer - layer, node.Pivot.Y);
                }
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

        private static void VisitIterative(
            Guid rootId,
            Dictionary<Guid, Dictionary<Guid, int>> graph,
            Dictionary<Guid, int> layers,
            Dictionary<Guid, NodeState> state,
            HashSet<Guid> cycles)
        {
            // Each stack frame remembers its remaining children to process and the current
            // best (max) child layer. When the enumerator is exhausted, we pop and assign.
            var stack = new Stack<Frame>();
            stack.Push(CreateFrame(rootId, graph));
            state[rootId] = NodeState.InProgress;

            while (stack.Count > 0)
            {
                var frame = stack.Peek();

                if (!frame.Children.MoveNext())
                {
                    // Post-order: all children processed. Leaves → layer 0.
                    var assigned = frame.MaxChildLayer < 0 ? 0 : frame.MaxChildLayer + 1;
                    layers[frame.NodeId] = assigned;
                    state[frame.NodeId] = NodeState.Done;
                    stack.Pop();

                    if (stack.Count > 0)
                    {
                        var parent = stack.Peek();
                        if (assigned > parent.MaxChildLayer)
                        {
                            // Frame is a reference type, so this mutates the frame instance
                            // still held on the stack — exactly what post-order accumulation needs.
                            parent.MaxChildLayer = assigned;
                        }
                    }

                    continue;
                }

                var childId = frame.Children.Current.Key;

                if (!state.TryGetValue(childId, out var childState))
                {
                    if (!graph.ContainsKey(childId))
                    {
                        // Dangling edge — ignore silently (validation catches this separately).
                        continue;
                    }

                    state[childId] = NodeState.InProgress;
                    stack.Push(CreateFrame(childId, graph));
                    continue;
                }

                switch (childState)
                {
                    case NodeState.Done:
                        if (layers.TryGetValue(childId, out var childLayer)
                            && childLayer > frame.MaxChildLayer)
                        {
                            frame.MaxChildLayer = childLayer;
                        }

                        break;

                    case NodeState.InProgress:
                        // Cycle edge: record both ends and treat as same-layer.
                        cycles.Add(frame.NodeId);
                        cycles.Add(childId);
                        break;
                }
            }
        }

        private static Frame CreateFrame(Guid nodeId, Dictionary<Guid, Dictionary<Guid, int>> graph)
        {
            var children = graph.TryGetValue(nodeId, out var map)
                ? map.GetEnumerator()
                : new Dictionary<Guid, int>().GetEnumerator();
            return new Frame(nodeId, children);
        }

        private enum NodeState
        {
            InProgress,
            Done,
        }

        private sealed class Frame
        {
            public Frame(Guid nodeId, IEnumerator<KeyValuePair<Guid, int>> children)
            {
                this.NodeId = nodeId;
                this.Children = children;
                this.MaxChildLayer = -1;
            }

            public Guid NodeId { get; }

            public IEnumerator<KeyValuePair<Guid, int>> Children { get; }

            public int MaxChildLayer { get; set; }
        }
    }
}
