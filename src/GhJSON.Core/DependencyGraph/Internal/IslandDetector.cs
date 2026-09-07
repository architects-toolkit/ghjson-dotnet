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

namespace GhJSON.Core.DependencyGraph.Internal
{
    internal static class IslandDetector
    {
        public static List<List<LayoutNode>> DetectIslands(List<LayoutNode> nodes)
        {
            var idToNode = nodes.ToDictionary(n => n.ComponentId, n => n);
            var visited = new HashSet<Guid>();
            var islands = new List<List<LayoutNode>>();

            foreach (var node in nodes)
            {
                if (visited.Contains(node.ComponentId))
                {
                    continue;
                }

                var stack = new Stack<Guid>();
                stack.Push(node.ComponentId);
                visited.Add(node.ComponentId);
                var island = new List<LayoutNode>();

                while (stack.Count > 0)
                {
                    var id = stack.Pop();
                    var n = idToNode[id];
                    island.Add(n);

                    foreach (var neighbor in n.Children.Keys.Concat(n.Parents.Keys))
                    {
                        if (!visited.Contains(neighbor) && idToNode.ContainsKey(neighbor))
                        {
                            visited.Add(neighbor);
                            stack.Push(neighbor);
                        }
                    }
                }

                islands.Add(island);
            }

            return islands;
        }
    }
}
