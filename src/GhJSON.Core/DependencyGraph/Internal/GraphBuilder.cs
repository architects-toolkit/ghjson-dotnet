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

namespace GhJSON.Core.DependencyGraph.Internal
{
    internal static class GraphBuilder
    {
        public static List<LayoutNode> BuildGraph(GhJsonDocument document)
        {
            var nodes = document.Components.Select(c => new LayoutNode
            {
                ComponentId = c.InstanceGuid.GetValueOrDefault(),
                Pivot = c.Pivot?.ToPointF() ?? PointF.Empty,
                Parents = new Dictionary<Guid, int>(),
                Children = new Dictionary<Guid, int>(),
            }).ToList();

            var idToGuidMap = document.GetIdToGuidMapping();
            if (document.Connections == null)
            {
                return nodes;
            }

            foreach (var conn in document.Connections)
            {
                if (idToGuidMap.TryGetValue(conn.From.Id, out var fromGuid) &&
                    idToGuidMap.TryGetValue(conn.To.Id, out var toGuid))
                {
                    var toNode = nodes.FirstOrDefault(n => n.ComponentId == toGuid);
                    var fromNode = nodes.FirstOrDefault(n => n.ComponentId == fromGuid);

                    if (toNode != null && fromNode != null)
                    {
                        int inputIndex = conn.To.ParamIndex ?? -1;
                        toNode.Parents[fromGuid] = inputIndex;

                        int outputIndex = conn.From.ParamIndex ?? -1;
                        fromNode.Children[toGuid] = outputIndex;
                    }
                }
            }

            return nodes;
        }
    }
}
