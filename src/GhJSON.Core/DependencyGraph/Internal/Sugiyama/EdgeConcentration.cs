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
    internal static class EdgeConcentration
    {
        public static List<LayoutNode> InsertHubNodes(List<LayoutNode> nodes)
        {
            var newNodes = new List<LayoutNode>(nodes);
            var byLayer = nodes.GroupBy(n => n.Pivot.X).OrderBy(g => g.Key).ToList();

            for (int li = 0; li < byLayer.Count - 1; li++)
            {
                var leftLayer = byLayer[li].ToList();
                var rightIds = new HashSet<Guid>(byLayer[li + 1].Select(n => n.ComponentId));

                var groups = leftLayer.Select(n => new
                {
                    Node = n,
                    Targets = n.Children.Keys.Where(id => rightIds.Contains(id)).OrderBy(id => id).ToList(),
                })
                .GroupBy(x => string.Join(",", x.Targets))
                .Where(g => g.Count() > 1 && g.First().Targets.Count > 1);

                foreach (var group in groups)
                {
                    var sourceNodes = group.Select(x => x.Node).ToList();
                    var targetIds = group.First().Targets;

                    var hubNode = new LayoutNode
                    {
                        ComponentId = Guid.NewGuid(),
                        Pivot = new PointF(leftLayer[0].Pivot.X + 0.5f, 0),
                        Parents = new Dictionary<Guid, int>(),
                        Children = new Dictionary<Guid, int>(),
                    };

                    foreach (var source in sourceNodes)
                    {
                        foreach (var targetId in targetIds)
                        {
                            source.Children.Remove(targetId);
                        }
                    }

                    foreach (var target in newNodes.Where(n => targetIds.Contains(n.ComponentId)))
                    {
                        foreach (var source in sourceNodes)
                        {
                            target.Parents.Remove(source.ComponentId);
                        }
                    }

                    foreach (var source in sourceNodes)
                    {
                        source.Children[hubNode.ComponentId] = -1;
                        hubNode.Parents[source.ComponentId] = -1;
                    }

                    foreach (var target in newNodes.Where(n => targetIds.Contains(n.ComponentId)))
                    {
                        target.Parents[hubNode.ComponentId] = -1;
                        hubNode.Children[target.ComponentId] = -1;
                    }

                    newNodes.Add(hubNode);
                }
            }

            return newNodes;
        }
    }
}
