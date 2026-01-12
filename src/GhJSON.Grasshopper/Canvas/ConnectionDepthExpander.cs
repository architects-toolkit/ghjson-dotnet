/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
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
using GhJSON.Core.Models.Document;

namespace GhJSON.Grasshopper.Canvas
{
    /// <summary>
    /// Utility for expanding object sets by connection depth in the canvas graph.
    /// </summary>
    internal static class ConnectionDepthExpander
    {
        /// <summary>
        /// Expands a set of initial GUIDs by traversing connections up to the specified depth.
        /// </summary>
        /// <param name="document">The GhJSON document containing all objects and connections.</param>
        /// <param name="initialGuids">The starting set of GUIDs.</param>
        /// <param name="depth">The depth to expand (0 = no expansion, 1 = direct neighbors, etc.).</param>
        /// <returns>The expanded set of GUIDs including all objects within the specified connection depth.</returns>
        public static HashSet<Guid> ExpandByDepth(GhJsonDocument document, IEnumerable<Guid> initialGuids, int depth)
        {
            if (depth <= 0)
            {
                return new HashSet<Guid>(initialGuids);
            }

            // Build edge list from document connections
            var edges = ExtractEdges(document);

            // Build adjacency maps for efficient traversal
            var adjacency = BuildAdjacency(edges);

            // Perform breadth-first expansion
            var result = new HashSet<Guid>(initialGuids);
            var frontier = new HashSet<Guid>(initialGuids);

            for (int level = 0; level < depth; level++)
            {
                var nextFrontier = new HashSet<Guid>();

                foreach (var guid in frontier)
                {
                    if (adjacency.TryGetValue(guid, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            if (result.Add(neighbor))
                            {
                                nextFrontier.Add(neighbor);
                            }
                        }
                    }
                }

                if (nextFrontier.Count == 0)
                {
                    break; // No more nodes to expand
                }

                frontier = nextFrontier;
            }

            return result;
        }

        /// <summary>
        /// Extracts directed edges from document connections.
        /// </summary>
        private static List<(Guid from, Guid to)> ExtractEdges(GhJsonDocument document)
        {
            var edges = new List<(Guid, Guid)>();
            
            if (document?.Connections == null)
            {
                return edges;
            }

            var idToGuid = document.GetIdToGuidMapping();

            foreach (var conn in document.Connections)
            {
                if (conn.TryResolveGuids(idToGuid, out var fromGuid, out var toGuid))
                {
                    edges.Add((fromGuid, toGuid));
                }
            }

            return edges;
        }

        /// <summary>
        /// Builds an undirected adjacency map from edges (both directions).
        /// </summary>
        private static Dictionary<Guid, HashSet<Guid>> BuildAdjacency(List<(Guid from, Guid to)> edges)
        {
            var adjacency = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var (from, to) in edges)
            {
                // Forward edge
                if (!adjacency.TryGetValue(from, out var fromNeighbors))
                {
                    fromNeighbors = new HashSet<Guid>();
                    adjacency[from] = fromNeighbors;
                }
                fromNeighbors.Add(to);

                // Reverse edge (undirected traversal)
                if (!adjacency.TryGetValue(to, out var toNeighbors))
                {
                    toNeighbors = new HashSet<Guid>();
                    adjacency[to] = toNeighbors;
                }
                toNeighbors.Add(from);
            }

            return adjacency;
        }

        /// <summary>
        /// Filters document connections to only include those between objects in the allowed set.
        /// </summary>
        public static void TrimConnections(GhJsonDocument document, HashSet<Guid> allowedGuids)
        {
            if (document?.Connections == null)
            {
                return;
            }

            var idToGuid = document.GetIdToGuidMapping();

            document.Connections = document.Connections
                .Where(c => c.TryResolveGuids(idToGuid, out var fromGuid, out var toGuid) &&
                            allowedGuids.Contains(fromGuid) && allowedGuids.Contains(toGuid))
                .ToList();
        }
    }
}
