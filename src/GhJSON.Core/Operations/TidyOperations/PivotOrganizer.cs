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

using System.Collections.Generic;
using System.Linq;
using GhJSON.Core.Models.Document;
using GhJSON.Core.Serialization;

namespace GhJSON.Core.Operations.TidyOperations
{
    /// <summary>
    /// Reorganizes component pivots based on graph flow analysis.
    /// </summary>
    public class PivotOrganizer
    {
        private readonly PivotOrganizerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PivotOrganizer"/> class.
        /// </summary>
        /// <param name="options">Organizer options.</param>
        public PivotOrganizer(PivotOrganizerOptions? options = null)
        {
            _options = options ?? PivotOrganizerOptions.Default;
        }

        /// <summary>
        /// Reorganizes component pivots in the document based on graph flow.
        /// </summary>
        /// <param name="document">The document to reorganize.</param>
        /// <returns>Operation result.</returns>
        public TidyResult Organize(GhJsonDocument document)
        {
            var result = new TidyResult();

            if (document?.Components == null || document.Components.Count == 0)
                return result;

            // Analyze the graph structure
            var analyzer = new LayoutAnalyzer();
            var analysis = analyzer.Analyze(document);

            // Build ID to component mapping
            var idToComponent = document.Components.ToDictionary(c => c.Id, c => c);

            float currentY = _options.StartY;

            // Process each island separately
            foreach (var island in analysis.Islands)
            {
                var islandResult = OrganizeIsland(island, analysis, idToComponent);
                result.NodesOrganized += islandResult.NodesOrganized;

                // Apply positions with island offset
                foreach (var nodeId in island)
                {
                    if (idToComponent.TryGetValue(nodeId, out var component) &&
                        islandResult.NodePositions.TryGetValue(nodeId, out var position))
                    {
                        component.Pivot = new CompactPosition(position.X, position.Y + currentY);
                    }
                }

                // Calculate island height and add spacing
                if (islandResult.NodePositions.Count > 0)
                {
                    var maxY = islandResult.NodePositions.Values.Max(p => p.Y);
                    currentY += maxY + _options.IslandSpacing;
                }
            }

            result.Success = true;
            result.WasModified = result.NodesOrganized > 0;

            return result;
        }

        /// <summary>
        /// Organizes nodes within a single island.
        /// </summary>
        private IslandOrganizeResult OrganizeIsland(
            List<int> islandNodes,
            LayoutAnalysis analysis,
            Dictionary<int, Models.Components.ComponentProperties> idToComponent)
        {
            var result = new IslandOrganizeResult();

            // Group nodes by depth level
            var nodesByDepth = new Dictionary<int, List<int>>();
            foreach (var nodeId in islandNodes)
            {
                var depth = analysis.NodeDepths.TryGetValue(nodeId, out var d) ? d : 0;
                if (!nodesByDepth.ContainsKey(depth))
                {
                    nodesByDepth[depth] = new List<int>();
                }
                nodesByDepth[depth].Add(nodeId);
            }

            // Calculate positions level by level
            float currentX = _options.StartX;

            foreach (var depth in nodesByDepth.Keys.OrderBy(k => k))
            {
                var nodesAtDepth = nodesByDepth[depth];
                float currentY = 0;

                // Sort nodes at same depth for consistent ordering
                nodesAtDepth.Sort();

                foreach (var nodeId in nodesAtDepth)
                {
                    result.NodePositions[nodeId] = new Position(currentX, currentY);
                    result.NodesOrganized++;
                    currentY += _options.VerticalSpacing;
                }

                currentX += _options.HorizontalSpacing;
            }

            return result;
        }
    }

    /// <summary>
    /// Options for pivot organization.
    /// </summary>
    public class PivotOrganizerOptions
    {
        /// <summary>
        /// Gets or sets the horizontal spacing between depth levels.
        /// </summary>
        public float HorizontalSpacing { get; set; } = 200f;

        /// <summary>
        /// Gets or sets the vertical spacing between nodes at the same depth.
        /// </summary>
        public float VerticalSpacing { get; set; } = 100f;

        /// <summary>
        /// Gets or sets the spacing between disconnected islands.
        /// </summary>
        public float IslandSpacing { get; set; } = 150f;

        /// <summary>
        /// Gets or sets the starting X coordinate.
        /// </summary>
        public float StartX { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the starting Y coordinate.
        /// </summary>
        public float StartY { get; set; } = 0f;

        /// <summary>
        /// Gets the default options.
        /// </summary>
        public static PivotOrganizerOptions Default => new PivotOrganizerOptions();
    }

    /// <summary>
    /// Result of organizing an island.
    /// </summary>
    internal class IslandOrganizeResult
    {
        public Dictionary<int, Position> NodePositions { get; } = new Dictionary<int, Position>();
        public int NodesOrganized { get; set; }
    }

    /// <summary>
    /// Simple position struct for internal calculations.
    /// </summary>
    internal struct Position
    {
        public float X { get; }
        public float Y { get; }

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Result of a tidy operation.
    /// </summary>
    public class TidyResult
    {
        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets whether the document was modified.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes organized.
        /// </summary>
        public int NodesOrganized { get; set; }

        /// <summary>
        /// Gets or sets any error message.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
