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
using GhJSON.Core.Models.Components;
using GhJSON.Core.Models.Document;

namespace GhJSON.Core.Operations.TidyOperations
{
    /// <summary>
    /// Analyzes the graph structure of a GhJSON document for layout optimization.
    /// </summary>
    public class LayoutAnalyzer
    {
        /// <summary>
        /// Analyzes a document and returns layout information.
        /// </summary>
        /// <param name="document">The document to analyze.</param>
        /// <returns>Layout analysis result.</returns>
        public LayoutAnalysis Analyze(GhJsonDocument document)
        {
            var analysis = new LayoutAnalysis();

            if (document?.Components == null || document.Components.Count == 0)
                return analysis;

            // Build adjacency information
            var adjacency = BuildAdjacencyMap(document);

            // Identify source nodes (no inputs)
            analysis.SourceNodes = document.Components
                .Where(c => !adjacency.ContainsKey(c.Id) || adjacency[c.Id].Inputs.Count == 0)
                .Select(c => c.Id)
                .ToList();

            // Identify sink nodes (no outputs)
            analysis.SinkNodes = document.Components
                .Where(c => !adjacency.ContainsKey(c.Id) || adjacency[c.Id].Outputs.Count == 0)
                .Select(c => c.Id)
                .ToList();

            // Calculate depth for each node (longest path from any source)
            analysis.NodeDepths = CalculateDepths(document, adjacency);

            // Group nodes by depth level
            analysis.DepthLevels = analysis.NodeDepths
                .GroupBy(kv => kv.Value)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            // Identify disconnected islands
            analysis.Islands = FindIslands(document, adjacency);

            // Calculate statistics
            analysis.MaxDepth = analysis.NodeDepths.Any() ? analysis.NodeDepths.Values.Max() : 0;
            analysis.AverageConnectionsPerNode = document.Connections?.Count > 0 && document.Components.Count > 0
                ? (float)document.Connections.Count / document.Components.Count
                : 0;

            return analysis;
        }

        /// <summary>
        /// Builds an adjacency map for all components.
        /// </summary>
        private static Dictionary<int, NodeAdjacency> BuildAdjacencyMap(GhJsonDocument document)
        {
            var adjacency = new Dictionary<int, NodeAdjacency>();

            // Initialize all nodes
            foreach (var component in document.Components)
            {
                adjacency[component.Id] = new NodeAdjacency();
            }

            // Build connections
            if (document.Connections != null)
            {
                foreach (var conn in document.Connections)
                {
                    if (adjacency.ContainsKey(conn.From.Id))
                    {
                        adjacency[conn.From.Id].Outputs.Add(conn.To.Id);
                    }

                    if (adjacency.ContainsKey(conn.To.Id))
                    {
                        adjacency[conn.To.Id].Inputs.Add(conn.From.Id);
                    }
                }
            }

            return adjacency;
        }

        /// <summary>
        /// Calculates the depth of each node (longest path from any source).
        /// </summary>
        private static Dictionary<int, int> CalculateDepths(GhJsonDocument document, Dictionary<int, NodeAdjacency> adjacency)
        {
            var depths = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            // Find source nodes
            var sources = document.Components
                .Where(c => !adjacency.ContainsKey(c.Id) || adjacency[c.Id].Inputs.Count == 0)
                .Select(c => c.Id)
                .ToList();

            // BFS from each source to calculate depths
            foreach (var sourceId in sources)
            {
                var queue = new Queue<(int id, int depth)>();
                queue.Enqueue((sourceId, 0));

                while (queue.Count > 0)
                {
                    var (nodeId, depth) = queue.Dequeue();

                    // Update depth if this path is longer
                    if (!depths.ContainsKey(nodeId) || depths[nodeId] < depth)
                    {
                        depths[nodeId] = depth;
                    }

                    // Visit outputs
                    if (adjacency.ContainsKey(nodeId))
                    {
                        foreach (var outputId in adjacency[nodeId].Outputs)
                        {
                            queue.Enqueue((outputId, depth + 1));
                        }
                    }
                }
            }

            // Handle disconnected nodes (assign depth 0)
            foreach (var component in document.Components)
            {
                if (!depths.ContainsKey(component.Id))
                {
                    depths[component.Id] = 0;
                }
            }

            return depths;
        }

        /// <summary>
        /// Finds disconnected component islands.
        /// </summary>
        private static List<List<int>> FindIslands(GhJsonDocument document, Dictionary<int, NodeAdjacency> adjacency)
        {
            var islands = new List<List<int>>();
            var visited = new HashSet<int>();

            foreach (var component in document.Components)
            {
                if (visited.Contains(component.Id))
                    continue;

                var island = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(component.Id);

                while (queue.Count > 0)
                {
                    var nodeId = queue.Dequeue();
                    if (visited.Contains(nodeId))
                        continue;

                    visited.Add(nodeId);
                    island.Add(nodeId);

                    if (adjacency.ContainsKey(nodeId))
                    {
                        foreach (var inputId in adjacency[nodeId].Inputs)
                        {
                            if (!visited.Contains(inputId))
                                queue.Enqueue(inputId);
                        }

                        foreach (var outputId in adjacency[nodeId].Outputs)
                        {
                            if (!visited.Contains(outputId))
                                queue.Enqueue(outputId);
                        }
                    }
                }

                if (island.Count > 0)
                {
                    islands.Add(island);
                }
            }

            return islands;
        }
    }

    /// <summary>
    /// Adjacency information for a node.
    /// </summary>
    internal class NodeAdjacency
    {
        public List<int> Inputs { get; } = new List<int>();
        public List<int> Outputs { get; } = new List<int>();
    }

    /// <summary>
    /// Result of layout analysis.
    /// </summary>
    public class LayoutAnalysis
    {
        /// <summary>
        /// Gets or sets the list of source node IDs (no inputs).
        /// </summary>
        public List<int> SourceNodes { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the list of sink node IDs (no outputs).
        /// </summary>
        public List<int> SinkNodes { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the depth of each node (longest path from source).
        /// </summary>
        public Dictionary<int, int> NodeDepths { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Gets or sets nodes grouped by depth level.
        /// </summary>
        public Dictionary<int, List<int>> DepthLevels { get; set; } = new Dictionary<int, List<int>>();

        /// <summary>
        /// Gets or sets the disconnected component islands.
        /// </summary>
        public List<List<int>> Islands { get; set; } = new List<List<int>>();

        /// <summary>
        /// Gets or sets the maximum depth in the graph.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the average number of connections per node.
        /// </summary>
        public float AverageConnectionsPerNode { get; set; }
    }
}
