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
    internal static class LayerAssignment
    {
        public static void AssignLayers(List<LayoutNode> nodes)
        {
            var graph = nodes.ToDictionary(n => n.ComponentId, n => n.Children);
            var layers = new Dictionary<Guid, int>();

            int Dfs(Guid nodeId)
            {
                if (layers.TryGetValue(nodeId, out var layer))
                {
                    return layer;
                }

                var children = graph[nodeId];
                var computedLayer = children.Count == 0 ? 0 : children.Select(child => Dfs(child.Key)).Max() + 1;
                layers[nodeId] = computedLayer;
                return computedLayer;
            }

            foreach (var nodeId in graph.Keys)
            {
                Dfs(nodeId);
            }

            var maxLayer = layers.Values.DefaultIfEmpty(0).Max();
            foreach (var node in nodes)
            {
                if (layers.TryGetValue(node.ComponentId, out var layer))
                {
                    node.Pivot = new PointF(maxLayer - layer, node.Pivot.Y);
                }
            }
        }
    }
}
